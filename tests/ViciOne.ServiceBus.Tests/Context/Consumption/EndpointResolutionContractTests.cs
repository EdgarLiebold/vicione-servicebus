using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Mediator.Contexts;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class EndpointResolutionContractTests
{
    static readonly Uri ResponseAddress = new("loopback://localhost/resolution-response");
    static readonly Uri FaultAddress = new("loopback://localhost/resolution-fault");
    static readonly Uri ExplicitAddress = new("loopback://localhost/resolution-explicit");
    static readonly Guid RequestId = Guid.Parse("8dcc154c-265f-4886-9868-5d3b96ca76a0");
    static readonly Guid ExplicitRequestId = Guid.Parse("1f4f7b11-df4f-4178-bbca-57be94c615cc");
    static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(37);

    public static TheoryData<int> Routes => [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    public static IEnumerable<object[]> RoutesAndCompletion()
    {
        for (int route = 0; route <= 10; route++)
        {
            yield return [route, false];
            yield return [route, true];
        }
    }

    public static IEnumerable<object[]> RoutesAndOutcomes()
    {
        for (int route = 0; route <= 10; route++)
            for (int outcome = 0; outcome <= 2; outcome++)
                yield return [route, outcome];
    }

    [Theory]
    [MemberData(nameof(Routes))]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "every-route-rejects-null-resolution-task-synchronously")]
    public void NullResolutionTask_IsRejectedSynchronouslyOnEveryRoute(int route)
    {
        using var fixture = new Fixture(route);
        fixture.ResolutionTask = null;

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = ResolveAsync(fixture, route, TestContext.Current.CancellationToken);
        });

        Assert.Equal(TaskDiagnostic(route), failure.Message);
        AssertResolution(fixture, route, TestContext.Current.CancellationToken);
        Assert.Empty(fixture.Sends);
        Assert.Empty(fixture.Notifications);
    }

    [Theory]
    [MemberData(nameof(RoutesAndCompletion))]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "every-route-rejects-null-endpoint-in-fast-and-held-paths")]
    public async Task NullEndpoint_IsRejectedForImmediateAndHeldResolutionAsync(int route, bool held)
    {
        using var fixture = new Fixture(route);
        var provider = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.ResolutionTask = held ? provider.Task : Task.FromResult<ISendEndpoint>(null!);
        Task<ISendEndpoint>? resolution = null;
        try
        {
            InvalidOperationException failure;
            if (held)
            {
                resolution = ResolveAsync(fixture, route, TestContext.Current.CancellationToken);
                Assert.False(resolution.IsCompleted);
                provider.SetResult(null!);
                failure = await Assert.ThrowsAsync<InvalidOperationException>(() => resolution);
            }
            else
            {
                failure = Assert.Throws<InvalidOperationException>(() =>
                {
                    _ = ResolveAsync(fixture, route, TestContext.Current.CancellationToken);
                });
            }
            Assert.Equal(EndpointDiagnostic(route), failure.Message);
            AssertResolution(fixture, route, TestContext.Current.CancellationToken);
            Assert.Empty(fixture.Sends);
            Assert.Empty(fixture.Notifications);
        }
        finally
        {
            provider.TrySetResult(null!);
            if (resolution is not null)
                await ObserveCompletionAsync(resolution);
        }
    }

    [Theory]
    [MemberData(nameof(Routes))]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "every-route-pre-cancellation-has-no-provider-effects")]
    public async Task PreCanceledResolution_NeverInvokesEitherProviderAsync(int route)
    {
        using var fixture = new Fixture(route);
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        fixture.ResolutionTask = null;

        Task<ISendEndpoint> resolution = ResolveAsync(fixture, route, caller.Token);

        Assert.True(resolution.IsCanceled);
        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resolution);
        Assert.Equal(caller.Token, failure.CancellationToken);
        Assert.Empty(fixture.Resolutions);
        Assert.Empty(fixture.Sends);
        Assert.Empty(fixture.Notifications);
    }

    [Theory]
    [MemberData(nameof(RoutesAndOutcomes))]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "every-route-owns-held-resolution-and-original-outcome")]
    public async Task EveryRoute_OwnsHeldResolutionAndPreservesItsOriginalOutcomeAsync(int route, int outcome)
    {
        using var fixture = new Fixture(route);
        using var caller = new CancellationTokenSource();
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var provider = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.ResolutionTask = provider.Task;
        var originalFailure = new InvalidOperationException("provider resolution failed");
        Task<ISendEndpoint>? resolution = null;
        try
        {
            resolution = ResolveAsync(fixture, route, caller.Token);
            Assert.False(resolution.IsCompleted);
            caller.Cancel();
            Assert.False(resolution.IsCompleted);
            AssertResolution(fixture, route, caller.Token);
            Assert.Empty(fixture.Sends);
            if (outcome == 0)
            {
                provider.SetResult(fixture.Endpoint);
                AssertEndpoint(fixture, route, await resolution.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None));
                Assert.True(resolution.IsCompletedSuccessfully);
            }
            else if (outcome == 1)
            {
                provider.SetException(originalFailure);
                Assert.Same(originalFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => resolution));
                Assert.True(resolution.IsFaulted);
            }
            else
            {
                provider.SetCanceled(providerCancellation.Token);
                OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resolution);
                Assert.Equal(providerCancellation.Token, canceled.CancellationToken);
                Assert.True(resolution.IsCanceled);
            }
            Assert.Empty(fixture.Sends);
            Assert.Empty(fixture.Notifications);
        }
        finally
        {
            provider.TrySetResult(fixture.Endpoint);
            await ObserveCompletionAsync(provider.Task);
            if (resolution is not null)
                await ObserveCompletionAsync(resolution);
        }
    }

    [Theory]
    [MemberData(nameof(RoutesAndCompletion))]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "every-route-preserves-endpoint-request-metadata-and-consume-ownership")]
    public async Task EveryRoute_DecoratesTheExactEndpointAndTransfersRequestMetadataAsync(int route, bool held)
    {
        using var fixture = new Fixture(route);
        var provider = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.ResolutionTask = held ? provider.Task : Task.FromResult(fixture.Endpoint);
        var dispatch = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.DispatchTask = dispatch.Task;
        Task<ISendEndpoint>? resolution = null;
        Task? send = null;
        Task? consumed = null;
        try
        {
            resolution = ResolveAsync(fixture, route, TestContext.Current.CancellationToken);
            if (held)
            {
                Assert.False(resolution.IsCompleted);
                provider.SetResult(fixture.Endpoint);
            }
            ISendEndpoint endpoint = await resolution.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            AssertEndpoint(fixture, route, endpoint);
            AssertResolution(fixture, route, TestContext.Current.CancellationToken);
            if (route == 10)
                return;

            var response = new ResolutionResponse("response");
            send = endpoint.SendAsync(response, TestContext.Current.CancellationToken);
            await fixture.SendEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            SendCall sent = Assert.Single(fixture.Sends);
            Assert.Same(response, sent.Message);
            Assert.Equal(TestContext.Current.CancellationToken, sent.CancellationToken);
            Assert.Equal(route is 1 or 4 or 5 or 6 or 9 ? ExplicitRequestId : RequestId, sent.Context.RequestId);
            Assert.Equal(Lifetime, sent.Context.TimeToLive);
            Assert.Equal(fixture.Owner.CorrelationId, sent.Context.InitiatorId);
            Assert.Equal("carried-header", sent.Context.Headers.Get<string>("resolution-header"));
            Assert.True(sent.Context.TryGetPayload(out ConsumeContext? inherited));
            Assert.Same(fixture.MessageContext, inherited);
            Assert.False(send.IsCompleted);
            consumed = fixture.Owner.ConsumeCompleted;
            Assert.False(consumed.IsCompleted);
            dispatch.SetResult();
            await send.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            await consumed.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(send.IsCompletedSuccessfully);
            Assert.True(consumed.IsCompletedSuccessfully);
            Assert.Empty(fixture.Notifications);
        }
        finally
        {
            provider.TrySetResult(fixture.Endpoint);
            dispatch.TrySetResult();
            if (resolution is not null)
                await ObserveCompletionAsync(resolution);
            if (send is not null)
                await ObserveCompletionAsync(send);
            if (consumed is not null)
                await ObserveCompletionAsync(consumed);
        }
    }

    [Theory]
    [MemberData(nameof(RoutesAndOutcomes))]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "every-route-preserves-immediate-provider-exception-or-cancellation")]
    public async Task ImmediateProviderFailure_PreservesTheOriginalExceptionOrCancellationAsync(int route, int outcome)
    {
        using var fixture = new Fixture(route);
        using var providerCancellation = new CancellationTokenSource();
        providerCancellation.Cancel();
        var originalFailure = new InvalidOperationException("immediate provider resolution failure");
        if (outcome == 0)
        {
            fixture.ProviderException = originalFailure;
            Assert.Same(originalFailure, Assert.Throws<InvalidOperationException>(() =>
            {
                _ = ResolveAsync(fixture, route, TestContext.Current.CancellationToken);
            }));
        }
        else
        {
            fixture.ResolutionTask = outcome == 1
                ? Task.FromException<ISendEndpoint>(originalFailure)
                : Task.FromCanceled<ISendEndpoint>(providerCancellation.Token);
            Task<ISendEndpoint> resolution = ResolveAsync(fixture, route, TestContext.Current.CancellationToken);
            if (outcome == 1)
            {
                Assert.Same(originalFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => resolution));
                Assert.True(resolution.IsFaulted);
            }
            else
            {
                OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resolution);
                Assert.Equal(providerCancellation.Token, canceled.CancellationToken);
                Assert.True(resolution.IsCanceled);
            }
        }
        AssertResolution(fixture, route, TestContext.Current.CancellationToken);
        Assert.Empty(fixture.Sends);
        Assert.Empty(fixture.Notifications);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "all-public-required-arguments-win-before-cancellation")]
    public void RequiredArguments_AreRejectedBeforeCancellation(bool cancel)
    {
        using var fixture = new Fixture(0);
        using var caller = new CancellationTokenSource();
        if (cancel)
            caller.Cancel();
        Action[] missingContext =
        [
            () => { _ = ConsumeContextEndpointExtensions.GetResponseEndpointAsync<ResolutionResponse>(null!, caller.Token); },
            () => { _ = ConsumeContextEndpointExtensions.GetResponseEndpointAsync<ResolutionResponse>(null!, null!, cancellationToken: caller.Token); },
            () => { _ = ConsumeContextEndpointExtensions.GetFaultEndpointAsync<ResolutionRequest>(null!, caller.Token); },
            () => { _ = ConsumeContextEndpointExtensions.GetFaultEndpointAsync<Fault<ResolutionRequest>>(null!, null!, cancellationToken: caller.Token); },
            () => { _ = ConsumeContextEndpointExtensions.GetReceiveFaultEndpointAsync(null!, null, null, caller.Token); },
        ];
        foreach (Action call in missingContext)
            Assert.Equal("context", Assert.Throws<ArgumentNullException>(call).ParamName);
        Assert.Equal("responseAddress", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = fixture.MessageContext.Advanced().GetResponseEndpointAsync<ResolutionResponse>(null!, cancellationToken: caller.Token);
        }).ParamName);
        Assert.Equal("faultAddress", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = fixture.MessageContext.Advanced().GetFaultEndpointAsync<Fault<ResolutionRequest>>(null!, cancellationToken: caller.Token);
        }).ParamName);
        Assert.Empty(fixture.Resolutions);
        Assert.Empty(fixture.Sends);
        Assert.Empty(fixture.Notifications);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "explicit-address-with-omitted-request-id-inherits-consumed-request")]
    public async Task ExplicitAddress_WithOmittedRequestIdInheritsTheConsumedRequestAsync(bool faulted, bool held)
    {
        int route = faulted ? 4 : 1;
        using var fixture = new Fixture(route);
        var provider = new TaskCompletionSource<ISendEndpoint>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.ResolutionTask = held ? provider.Task : Task.FromResult(fixture.Endpoint);
        Task<ISendEndpoint>? resolution = null;
        Task? send = null;
        try
        {
            resolution = faulted
                ? fixture.MessageContext.Advanced().GetFaultEndpointAsync<Fault<ResolutionRequest>>(
                    ExplicitAddress, cancellationToken: TestContext.Current.CancellationToken)
                : fixture.MessageContext.Advanced().GetResponseEndpointAsync<ResolutionResponse>(
                    ExplicitAddress, cancellationToken: TestContext.Current.CancellationToken);
            if (held)
            {
                Assert.False(resolution.IsCompleted);
                provider.SetResult(fixture.Endpoint);
            }
            ISendEndpoint endpoint = await resolution.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            AssertEndpoint(fixture, route, endpoint);
            AssertResolution(fixture, route, TestContext.Current.CancellationToken);
            send = endpoint.SendAsync(new ResolutionResponse("inherited request"), TestContext.Current.CancellationToken);
            await send.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            SendCall sent = Assert.Single(fixture.Sends);
            Assert.Equal(RequestId, sent.Context.RequestId);
            Assert.Equal(Lifetime, sent.Context.TimeToLive);
            Assert.Empty(fixture.Notifications);
        }
        finally
        {
            provider.TrySetResult(fixture.Endpoint);
            if (resolution is not null)
                await ObserveCompletionAsync(resolution);
            if (send is not null)
                await ObserveCompletionAsync(send);
        }
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(8, false)]
    [InlineData(8, true)]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "default-fault-generation-owns-dispatch-before-receive-notification")]
    public async Task DefaultFaultGeneration_AwaitsTheActualDispatchBeforeReceiveNotificationAsync(int route, bool cancelCaller)
    {
        using var fixture = new Fixture(route);
        using var caller = new CancellationTokenSource();
        var dispatch = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.DispatchTask = dispatch.Task;
        var consumerFailure = new InvalidOperationException("actual default fault route");
        Task? notification = null;
        try
        {
            notification = fixture.Owner.NotifyFaultedAsync(fixture.MessageContext, TimeSpan.FromTicks(123),
                "default-fault-consumer", consumerFailure, caller.Token);
            await fixture.SendEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(notification.IsCompleted);
            Assert.Empty(fixture.Notifications);
            Assert.False(fixture.Receive.IsFaulted);
            AssertResolution(fixture, route, fixture.MessageContext.CancellationToken);
            SendCall sent = Assert.Single(fixture.Sends);
            Fault<ResolutionRequest> fault = Assert.IsAssignableFrom<Fault<ResolutionRequest>>(sent.Message);
            Assert.Same(fixture.MessageContext.Message, fault.Message);
            Assert.Equal(fixture.MessageContext.MessageId, fault.FaultedMessageId);
            Assert.Equal(fixture.Timestamp, fault.Timestamp);
            Assert.NotEqual(Guid.Empty, fault.FaultId);
            Assert.NotNull(fault.Host);
            Assert.Equal(fixture.MessageContext.Advanced().SupportedMessageTypes, fault.FaultMessageTypes);
            Assert.Contains(fault.Exceptions, exception => exception.Message == consumerFailure.Message);
            Assert.Equal(fixture.MessageContext.CancellationToken, sent.CancellationToken);
            Assert.Equal(RequestId, sent.Context.RequestId);
            Assert.Equal(fixture.MessageContext.CorrelationId, sent.Context.CorrelationId);
            Assert.Equal(Lifetime, sent.Context.TimeToLive);
            if (cancelCaller)
                caller.Cancel();
            Assert.False(notification.IsCompleted);
            Assert.Empty(fixture.Notifications);
            dispatch.SetResult();
            if (cancelCaller)
            {
                OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => notification);
                Assert.Equal(caller.Token, canceled.CancellationToken);
                Assert.True(notification.IsCanceled);
                Assert.False(fixture.Receive.IsFaulted);
                Assert.Empty(fixture.Notifications);
            }
            else
            {
                await notification.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                ObserverCall observed = Assert.Single(fixture.Notifications);
                Assert.Same(fixture.MessageContext, observed.Context);
                Assert.Same(consumerFailure, observed.Exception);
                Assert.Equal(TimeSpan.FromTicks(123), observed.Duration);
                Assert.Equal("default-fault-consumer", observed.ConsumerType);
                Assert.True(observed.IsFaulted);
                Assert.True(fixture.Receive.IsFaulted);
            }
        }
        finally
        {
            dispatch.TrySetResult();
            if (notification is not null)
                await ObserveCompletionAsync(notification);
            await ObserveCompletionAsync(fixture.Owner.ConsumeCompleted);
        }
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "default-fault-route-failure-never-notifies-receive")]
    public async Task DefaultFaultRouteFailure_NeverNotifiesReceiveAsync(bool publish, int phase)
    {
        int route = publish ? 8 : 2;
        using var fixture = new Fixture(route);
        var originalFailure = new InvalidOperationException("default fault route provider failure");
        fixture.ResolutionTask = phase switch
        {
            0 => null,
            1 => Task.FromResult<ISendEndpoint>(null!),
            2 => Task.FromException<ISendEndpoint>(originalFailure),
            _ => Task.FromResult(fixture.Endpoint),
        };
        if (phase == 3)
            fixture.DispatchTask = Task.FromException(originalFailure);

        Task notification = fixture.Owner.NotifyFaultedAsync(fixture.MessageContext, TimeSpan.Zero,
            "default-fault-consumer", new InvalidOperationException("consumer failed"), TestContext.Current.CancellationToken);
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => notification);

        if (phase <= 1)
            Assert.Equal(phase == 0 ? TaskDiagnostic(route) : EndpointDiagnostic(route), failure.Message);
        else
            Assert.Same(originalFailure, failure);
        AssertResolution(fixture, route, fixture.MessageContext.CancellationToken);
        Assert.Equal(phase == 3 ? 1 : 0, fixture.Sends.Count);
        Assert.Empty(fixture.Notifications);
        Assert.False(fixture.Receive.IsFaulted);
        Assert.False(fixture.Receive.TryGetPayload(out ConsumerFaultContext? _));
        await ObserveCompletionAsync(fixture.Owner.ConsumeCompleted);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "disabled-publication-preserves-explicit-fault-and-response-routing")]
    public async Task DisabledFaultPublication_PreservesAddressedFaultsWithoutPublishingUnaddressedFaultsAsync(int route)
    {
        using var fixture = new Fixture(route) { PublishFaults = false };
        var failure = new InvalidOperationException("consumer failed");

        await fixture.Owner.NotifyFaultedAsync(fixture.MessageContext, TimeSpan.Zero,
            "default-fault-consumer", failure, TestContext.Current.CancellationToken);

        if (route == 8)
        {
            Assert.Empty(fixture.Resolutions);
            Assert.Empty(fixture.Sends);
        }
        else
        {
            AssertResolution(fixture, route, fixture.MessageContext.CancellationToken);
            Assert.Single(fixture.Sends);
        }
        Assert.Same(failure, Assert.Single(fixture.Notifications).Exception);
        Assert.True(fixture.Receive.IsFaulted);
    }

    static Task<ISendEndpoint> ResolveAsync(Fixture fixture, int route, CancellationToken cancellationToken) => route switch
    {
        0 or 7 => fixture.MessageContext.Advanced().GetResponseEndpointAsync<ResolutionResponse>(cancellationToken),
        1 => fixture.MessageContext.Advanced().GetResponseEndpointAsync<ResolutionResponse>(ExplicitAddress, ExplicitRequestId, cancellationToken),
        2 or 3 or 8 => fixture.MessageContext.Advanced().GetFaultEndpointAsync<ResolutionRequest>(cancellationToken),
        4 => fixture.MessageContext.Advanced().GetFaultEndpointAsync<Fault<ResolutionRequest>>(ExplicitAddress, ExplicitRequestId, cancellationToken),
        5 or 6 or 9 => fixture.Receive.GetReceiveFaultEndpointAsync(fixture.MessageContext.Advanced(), ExplicitRequestId, cancellationToken),
        10 => fixture.Receive.GetReceiveFaultEndpointAsync(null, ExplicitRequestId, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(route)),
    };

    static Uri? Destination(int route) => route switch
    {
        0 or 3 or 6 => ResponseAddress,
        1 or 4 => ExplicitAddress,
        2 or 5 => FaultAddress,
        _ => null,
    };

    static string TaskDiagnostic(int route) => route >= 7
        ? "The publish endpoint provider returned no endpoint resolution task."
        : $"The send endpoint provider returned no endpoint resolution task for '{Destination(route)}'.";

    static string EndpointDiagnostic(int route) => route >= 7
        ? "The publish endpoint provider resolved no send endpoint."
        : $"The send endpoint provider resolved no send endpoint for '{Destination(route)}'.";

    static void AssertResolution(Fixture fixture, int route, CancellationToken cancellationToken)
    {
        ResolutionCall call = Assert.Single(fixture.Resolutions);
        Assert.Equal(route >= 7, call.Published);
        Assert.Equal(Destination(route), call.Address);
        Assert.Equal(cancellationToken, call.CancellationToken);
        Type? publishedType = route switch
        {
            7 => typeof(ResolutionResponse),
            8 => typeof(Fault<ResolutionRequest>),
            9 or 10 => typeof(ReceiveFault),
            _ => null,
        };
        Assert.Equal(publishedType, call.PublishedType);
    }

    static void AssertEndpoint(Fixture fixture, int route, ISendEndpoint endpoint)
    {
        if (route == 10)
            Assert.Same(fixture.Endpoint, endpoint);
        else
            Assert.Same(fixture.Endpoint, Assert.IsType<ConsumeSendEndpoint>(endpoint).Endpoint);
    }

    static async Task ObserveCompletionAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        }
        catch (Exception) when (task.IsCompleted)
        {
            // Settled failures are observed during cleanup; incomplete operations still time out.
        }
    }

    sealed class Fixture : IDisposable
    {
        public Fixture(int route)
        {
            var message = new ResolutionRequest("request");
            var send = new MessageSendContext<ResolutionRequest>(message, TestContext.Current.CancellationToken)
            {
                DestinationAddress = new Uri("loopback://localhost/resolution-input"),
                ResponseAddress = route is 0 or 2 or 3 or 5 or 6 ? ResponseAddress : null,
                FaultAddress = route is 2 or 5 or 7 ? FaultAddress : null,
                RequestId = RequestId,
                CorrelationId = Guid.Parse("8c27732c-68cb-426e-82e3-829b4df8c5bd"),
                TimeToLive = Lifetime,
            };
            send.Headers.Set("resolution-header", "carried-header");
            Timestamp = send.SentTime!.Value;
            send.GetOrAddPayload<TimeProvider>(() => new FakeTimeProvider(Timestamp));
            Endpoint = StrictProxy.Create<ITransportSendEndpoint>(DispatchEndpoint);
            ResolutionTask = Task.FromResult(Endpoint);
            ISendEndpointProvider sendProvider = StrictProxy.Create<ISendEndpointProvider>((method, args) =>
            {
                if (method.Name != nameof(ISendEndpointProvider.GetSendEndpointAsync))
                    throw new InvalidOperationException($"Unexpected send provider operation: {method.Name}.");
                Resolutions.Add(new ResolutionCall(false, (Uri)args![0]!, null, (CancellationToken)args[1]!));
                if (ProviderException is { } failure)
                    throw failure;
                return ResolutionTask;
            });
            IPublishEndpointProvider publishProvider = StrictProxy.Create<IPublishEndpointProvider>((method, args) =>
            {
                if (method.Name != nameof(IPublishEndpointProvider.GetPublishSendEndpointAsync))
                    throw new InvalidOperationException($"Unexpected publish provider operation: {method.Name}.");
                Resolutions.Add(new ResolutionCall(true, null, method.GetGenericArguments()[0], (CancellationToken)args![0]!));
                if (ProviderException is { } failure)
                    throw failure;
                return ResolutionTask;
            });
            IReceiveObserver observer = StrictProxy.Create<IReceiveObserver>((method, args) =>
            {
                if (method.Name != nameof(IReceiveObserver.ConsumeFaultAsync))
                    throw new InvalidOperationException($"Unexpected receive observer operation: {method.Name}.");
                Notifications.Add(new ObserverCall(args![0]!, (TimeSpan)args[1]!, (string)args[2]!,
                    (Exception)args[3]!, (Receive ?? throw new InvalidOperationException("The receive context is not initialized.")).IsFaulted));
                return Task.CompletedTask;
            });
            ReceiveEndpointContext endpoint = StrictProxy.Create<ReceiveEndpointContext>((method, args) => method.Name switch
            {
                "get_InputAddress" => send.DestinationAddress,
                "get_PublishFaults" => PublishFaults,
                "get_SendEndpointProvider" => sendProvider,
                "get_PublishEndpointProvider" => publishProvider,
                "get_ReceiveObservers" => observer,
                nameof(PipeContext.TryGetPayload) => method.Invoke(send, args),
                _ => throw new InvalidOperationException($"Unexpected receive endpoint operation: {method.Name}."),
            });
            Receive = new TransportContext(endpoint);
            var metadata = new MediatorSendMessageContext<ResolutionRequest>(send);
            var serialization = new MediatorSerializationContext<ResolutionRequest>(ServiceBusMetadataJson.ObjectDeserializer,
                metadata, message, send.SupportedMessageTypes);
            Owner = new ResolutionContext(Receive, serialization);
            MessageContext = new MessageConsumeContext<ResolutionRequest>(Owner, message);
        }

        public ISendEndpoint Endpoint { get; }
        public DateTimeOffset Timestamp { get; }
        public Task<ISendEndpoint>? ResolutionTask { get; set; }
        public Exception? ProviderException { get; set; }
        public Task DispatchTask { get; set; } = Task.CompletedTask;
        public bool PublishFaults { get; set; } = true;
        public TransportContext Receive { get; }
        public ResolutionContext Owner { get; }
        public ConsumeContext<ResolutionRequest> MessageContext { get; }
        public List<ResolutionCall> Resolutions { get; } = [];
        public List<SendCall> Sends { get; } = [];
        public List<ObserverCall> Notifications { get; } = [];
        public TaskCompletionSource SendEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        object? DispatchEndpoint(MethodInfo method, object?[]? args)
        {
            if (method.Name != nameof(ISendEndpoint.SendAsync) || !method.IsGenericMethod || args?.Length != 3)
                throw new InvalidOperationException($"Unexpected endpoint operation: {method.Name}.");
            MethodInfo record = typeof(Fixture).GetMethod(nameof(RecordSendAsync), BindingFlags.Instance | BindingFlags.NonPublic)!;
            return record.MakeGenericMethod(method.GetGenericArguments()).Invoke(this, args);
        }

        async Task RecordSendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
            where T : class
        {
            var sent = new MessageSendContext<T>(message, cancellationToken);
            if (pipe is ISendContextPipe general)
                await general.SendAsync(sent, cancellationToken);
            await pipe.SendAsync(sent);
            Sends.Add(new SendCall(message, sent, cancellationToken));
            SendEntered.TrySetResult();
            await DispatchTask;
        }

        public void Dispose() => Receive.Dispose();
    }

    sealed class ResolutionContext(ReceiveContext receive, SerializerContext serialization) : DeserializerConsumeContext(receive, serialization)
    {
        public override Guid? MessageId => SerializerContext.MessageId;
        public override Guid? RequestId => SerializerContext.RequestId;
        public override Guid? CorrelationId => SerializerContext.CorrelationId;
        public override Guid? ConversationId => SerializerContext.ConversationId;
        public override Guid? InitiatorId => SerializerContext.InitiatorId;
        public override DateTimeOffset? ExpirationTime => SerializerContext.ExpirationTime;
        public override Uri? SourceAddress => SerializerContext.SourceAddress;
        public override Uri? DestinationAddress => SerializerContext.DestinationAddress;
        public override Uri? ResponseAddress => SerializerContext.ResponseAddress;
        public override Uri? FaultAddress => SerializerContext.FaultAddress;
        public override DateTimeOffset? SentTime => SerializerContext.SentTime;
        public override Headers Headers => SerializerContext.Headers;
        public override HostInfo Host => SerializerContext.Host;
        public override IEnumerable<string> SupportedMessageTypes => SerializerContext.SupportedMessageTypes;
        public override bool HasMessageType(Type messageType) => SerializerContext.IsSupportedMessageType(messageType);
        public override bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
            where T : class
        {
            if (SerializerContext.TryGetMessage(out T? message))
            {
                consumeContext = new MessageConsumeContext<T>(this, message);
                return true;
            }
            consumeContext = null;
            return false;
        }
    }

    sealed class TransportContext(ReceiveEndpointContext endpoint) : BaseReceiveContext(false, endpoint)
    {
        protected override IHeaderProvider HeaderProvider => new DictionarySendHeaderProvider(new DictionarySendHeaders());
        public override MessageBody Body => new BinaryMessageBody("{}"u8.ToArray());
    }

    class StrictProxy : DispatchProxy
    {
        Func<MethodInfo, object?[]?, object?>? _dispatch;
        public static T Create<T>(Func<MethodInfo, object?[]?, object?> dispatch)
            where T : class
        {
            T proxy = DispatchProxy.Create<T, StrictProxy>();
            ((StrictProxy)(object)proxy)._dispatch = dispatch;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return (_dispatch ?? throw new InvalidOperationException("Dependency dispatch is not configured."))(targetMethod, args);
        }
    }

    sealed record ResolutionRequest(string Value);
    sealed record ResolutionResponse(string Value);
    sealed record ResolutionCall(bool Published, Uri? Address, Type? PublishedType, CancellationToken CancellationToken);
    sealed record SendCall(object Message, SendContext Context, CancellationToken CancellationToken);
    sealed record ObserverCall(object Context, TimeSpan Duration, string ConsumerType, Exception Exception, bool IsFaulted);
}
