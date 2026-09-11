using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Context.Consumption;

public sealed class ConsumeContextEndpointExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "all-resolution-paths-forward-cancellation-token")]
    public async Task EveryResponseAndFaultResolutionPath_ForwardsTheExactCancellationTokenAsync()
    {
        var endpoint = new RecordingSendEndpoint();
        var sendProvider = new RecordingSendEndpointProvider(endpoint);
        var publishProvider = new RecordingPublishEndpointProvider(endpoint);
        var receiveContext = new EndpointReceiveContext(sendProvider, publishProvider);
        Uri responseAddress = new("loopback://localhost/context-response");
        Uri faultAddress = new("loopback://localhost/context-fault");
        var consumeContext = new EndpointConsumeContext(receiveContext, responseAddress, faultAddress);
        using var cancellation = new CancellationTokenSource();

        Assert.NotNull(await consumeContext.GetResponseEndpointAsync<EndpointResponse>(cancellation.Token));
        Assert.Equal(cancellation.Token, Assert.Single(sendProvider.Resolutions).CancellationToken);

        Uri explicitResponseAddress = new("loopback://localhost/explicit-response");
        Assert.NotNull(await consumeContext.GetResponseEndpointAsync<EndpointResponse>(
            explicitResponseAddress,
            cancellationToken: cancellation.Token));
        Assert.Equal(
            (explicitResponseAddress, cancellation.Token),
            (sendProvider.Resolutions[1].Address, sendProvider.Resolutions[1].CancellationToken));

        Assert.NotNull(await consumeContext.GetFaultEndpointAsync<EndpointRequest>(cancellation.Token));
        Assert.Equal(cancellation.Token, sendProvider.Resolutions[2].CancellationToken);

        Uri explicitFaultAddress = new("loopback://localhost/explicit-fault");
        Assert.NotNull(await consumeContext.GetFaultEndpointAsync<Fault<EndpointRequest>>(
            explicitFaultAddress,
            cancellationToken: cancellation.Token));
        Assert.Equal(
            (explicitFaultAddress, cancellation.Token),
            (sendProvider.Resolutions[3].Address, sendProvider.Resolutions[3].CancellationToken));

        Assert.NotNull(await receiveContext.GetReceiveFaultEndpointAsync(
            consumeContext,
            Guid.Parse("a86fca20-bfab-4af0-9c0a-93d08387f3a7"),
            cancellation.Token));
        Assert.Equal(cancellation.Token, sendProvider.Resolutions[4].CancellationToken);

        Assert.NotNull(await receiveContext.GetReceiveFaultEndpointAsync(
            consumeContext: null,
            requestId: null,
            cancellation.Token));
        Assert.Equal(cancellation.Token, Assert.Single(publishProvider.Resolutions));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTEXT-ENDPOINT", "required-context-and-explicit-address-boundaries")]
    public void EndpointResolution_RejectsMissingContextsAndExplicitAddressesSynchronously()
    {
        var endpoint = new RecordingSendEndpoint();
        var receiveContext = new EndpointReceiveContext(
            new RecordingSendEndpointProvider(endpoint),
            new RecordingPublishEndpointProvider(endpoint));
        var consumeContext = new EndpointConsumeContext(receiveContext, responseAddress: null, faultAddress: null);

        void ResolveResponseWithoutContext() => _ = ConsumeContextEndpointExtensions.GetResponseEndpointAsync<EndpointResponse>(
            null!,
            TestContext.Current.CancellationToken);
        void ResolveFaultWithoutContext() => _ = ConsumeContextEndpointExtensions.GetFaultEndpointAsync<EndpointRequest>(
            null!,
            TestContext.Current.CancellationToken);
        void ResolveReceiveFaultWithoutContext() => _ = ConsumeContextEndpointExtensions.GetReceiveFaultEndpointAsync(
            null!,
            null,
            null,
            TestContext.Current.CancellationToken);
        void ResolveWithoutResponseAddress() => _ = consumeContext.GetResponseEndpointAsync<EndpointResponse>(
            null!,
            cancellationToken: TestContext.Current.CancellationToken);
        void ResolveWithoutFaultAddress() => _ = consumeContext.GetFaultEndpointAsync<EndpointRequest>(
            null!,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(ResolveResponseWithoutContext).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(ResolveFaultWithoutContext).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(ResolveReceiveFaultWithoutContext).ParamName);
        Assert.Equal("responseAddress", Assert.Throws<ArgumentNullException>(ResolveWithoutResponseAddress).ParamName);
        Assert.Equal("faultAddress", Assert.Throws<ArgumentNullException>(ResolveWithoutFaultAddress).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POLYMORPHIC-FAULT-PUBLICATION", "derived-interface-to-base-fault")]
    public async Task ThrownDerivedInterfaceMessage_PublishesAConsumableBaseFaultAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"fault-publishing-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Handler<UpdateMemberAddressCommand>(
                _ => Task.FromException(new ExpectedHandlerException()));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        try
        {
            await harness.StartAsync(cancellationToken);
            var receivedBaseFault = new TaskCompletionSource<ConsumeContext<Fault<MemberUpdateCommand>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            IHostReceiveEndpointHandle faultEndpoint = harness.Bus.ConnectReceiveEndpoint(configurator =>
                configurator.Handler<Fault<MemberUpdateCommand>>(context =>
                {
                    receivedBaseFault.TrySetResult(context);
                    return Task.CompletedTask;
                }));
            await faultEndpoint.Ready.WaitAsync(harness.TestTimeout, cancellationToken);

            try
            {
                await harness.InputQueueSendEndpoint.SendAsync<UpdateMemberAddressCommand>(
                    new
                    {
                        MemberName = "Frank",
                        Address = "123 American Way",
                    },
                    cancellationToken);

                ConsumeContext<Fault<MemberUpdateCommand>> context = await receivedBaseFault.Task.WaitAsync(
                    harness.TestTimeout,
                    cancellationToken);

                Assert.Equal("Frank", context.Message.Message.MemberName);
                Assert.True(context.TryGetMessage(out ConsumeContext<Fault<UpdateMemberAddressCommand>>? derivedFault));
                Assert.Equal("Frank", derivedFault.Message.Message.MemberName);
                Assert.Equal("123 American Way", derivedFault.Message.Message.Address);
                Assert.NotEqual(Guid.Empty, context.Message.FaultId);
                Assert.NotNull(context.Message.Host);
                Assert.Contains(
                    context.Message.Exceptions,
                    exception => exception.ExceptionType == TypeCache<ExpectedHandlerException>.ShortName);
                Assert.Equal(
                    new[]
                    {
                        MessageUrn.ForTypeString<ApplicationCommand>(),
                        MessageUrn.ForTypeString<MemberUpdateCommand>(),
                        MessageUrn.ForTypeString<UpdateMemberAddressCommand>(),
                    }.Order(StringComparer.Ordinal),
                    context.Message.FaultMessageTypes.Order(StringComparer.Ordinal));
                Assert.True(await harness.Published.AnyAsync<Fault<UpdateMemberAddressCommand>>(cancellationToken));
            }
            finally
            {
                await faultEndpoint.StopAsync(cancellationToken);
            }
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FAULT-NOTIFICATION", "derived-context-from-base-context")]
    public async Task DerivedContextObtainedFromBaseContext_PublishesOneCompleteFaultAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        using var harness = new InMemoryTestHarness($"try-get-fault-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
            TestInactivityTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var derivedContextObserved = new TaskCompletionSource<ConsumeContext<DerivedFaultCommand>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Handler<BaseFaultCommand>(context =>
            {
                if (!context.TryGetMessage(out ConsumeContext<DerivedFaultCommand>? derivedContext))
                    throw new InvalidOperationException("The derived consume context was not available.");

                derivedContextObserved.TrySetResult(derivedContext);
                return derivedContext.NotifyFaultedAsync(
                    TimeSpan.Zero,
                    TypeCache<ConsumeContextEndpointExtensionsTests>.ShortName,
                    new ExpectedDerivedFaultException());
            });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;
        IHostReceiveEndpointHandle? faultEndpoint = null;

        try
        {
            await harness.StartAsync(cancellationToken);
            started = true;
            var receivedBaseFault = new TaskCompletionSource<ConsumeContext<Fault<BaseFaultCommand>>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            faultEndpoint = harness.Bus.ConnectReceiveEndpoint(configurator =>
                configurator.Handler<Fault<BaseFaultCommand>>(context =>
                {
                    receivedBaseFault.TrySetResult(context);
                    return Task.CompletedTask;
                }));
            await faultEndpoint.Ready.WaitAsync(operationTimeout, cancellationToken);

            Guid commandId = Guid.Parse("856dd4c5-58c8-4530-8afc-58b7f35d274b");
            await harness.InputQueueSendEndpoint.SendAsync<DerivedFaultCommand>(
                new { CommandId = commandId, Value = "fault me" },
                cancellationToken);

            ConsumeContext<DerivedFaultCommand> derivedContext = await derivedContextObserved.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            ConsumeContext<Fault<BaseFaultCommand>> baseFault = await receivedBaseFault.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            Assert.True(baseFault.TryGetMessage(
                out ConsumeContext<Fault<DerivedFaultCommand>>? derivedFault));

            await faultEndpoint.StopAsync(CancellationToken.None);
            faultEndpoint = null;
            await harness.StopAsync(TestContext.Current.CancellationToken);
            started = false;

            Assert.Single(harness.Published.Snapshot<Fault<BaseFaultCommand>>());
            Assert.Single(harness.Published.Snapshot<Fault<DerivedFaultCommand>>());
            Assert.Equal(commandId, derivedContext.Message.CommandId);
            Assert.Equal("fault me", derivedContext.Message.Value);
            Assert.Equal(commandId, baseFault.Message.Message.CommandId);
            Assert.Equal(commandId, derivedFault.Message.Message.CommandId);
            Assert.Equal(
                new[]
                {
                    MessageUrn.ForTypeString<BaseFaultCommand>(),
                    MessageUrn.ForTypeString<DerivedFaultCommand>(),
                }.Order(StringComparer.Ordinal),
                baseFault.Message.FaultMessageTypes.Order(StringComparer.Ordinal));
            Assert.Contains(
                baseFault.Message.Exceptions,
                exception => exception.ExceptionType == TypeCache<ExpectedDerivedFaultException>.ShortName);
            Assert.Contains(
                MessageUrn.ForTypeString<Fault<BaseFaultCommand>>(),
                baseFault.Advanced().SupportedMessageTypes);
            Assert.Contains(
                MessageUrn.ForTypeString<Fault<DerivedFaultCommand>>(),
                baseFault.Advanced().SupportedMessageTypes);
        }
        finally
        {
            if (faultEndpoint is not null)
                await faultEndpoint.StopAsync(CancellationToken.None);
            if (started)
                await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private sealed class ExpectedHandlerException : Exception;

    private sealed class ExpectedDerivedFaultException : Exception;

    private sealed record EndpointRequest(string Value);

    private sealed record EndpointResponse(string Value);

    private sealed class EndpointConsumeContext : BaseConsumeContext
    {
        private readonly Uri _destinationAddress;

        public EndpointConsumeContext(ReceiveContext receiveContext, Uri? responseAddress, Uri? faultAddress)
            : base(receiveContext, CreateSerializerContext())
        {
            _destinationAddress = receiveContext.InputAddress;
            ResponseAddress = responseAddress;
            FaultAddress = faultAddress;
        }

        public override Task ConsumeCompleted => Task.CompletedTask;

        public override Guid? MessageId => null;

        public override Guid? RequestId { get; } = Guid.Parse("e27041c5-5203-4210-9228-8452a28447bf");

        public override Guid? CorrelationId => null;

        public override Guid? ConversationId => null;

        public override Guid? InitiatorId => null;

        public override DateTimeOffset? ExpirationTime => null;

        public override Uri? SourceAddress => null;

        public override Uri? DestinationAddress => _destinationAddress;

        public override Uri? ResponseAddress { get; }

        public override Uri? FaultAddress { get; }

        public override DateTimeOffset? SentTime => null;

        public override Headers Headers => throw new NotSupportedException();

        public override HostInfo Host => throw new NotSupportedException();

        public override IEnumerable<string> SupportedMessageTypes => [];

        public override bool HasMessageType(Type messageType) => false;

        public override bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
            where T : class
        {
            consumeContext = null;
            return false;
        }

        public override bool HasPayloadType(Type payloadType) => payloadType.IsInstanceOfType(this);

        public override bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
            where T : class
        {
            payload = this as T;
            return payload is not null;
        }

        public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
            where T : class => payloadFactory();

        public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
            where T : class => addFactory();

        public override void AddConsumeTask(Task task)
        {
        }

        private static SerializerContext CreateSerializerContext() =>
            DispatchProxy.Create<SerializerContext, UnsupportedSerializerContextProxy>();
    }

    private sealed class EndpointReceiveContext(
        ISendEndpointProvider sendEndpointProvider,
        IPublishEndpointProvider publishEndpointProvider) : BasePipeContext, ReceiveContext
    {
        public TimeSpan ElapsedTime => TimeSpan.Zero;

        public Uri InputAddress { get; } = new("loopback://localhost/context-input");

        public ContentType ContentType => throw new NotSupportedException();

        public bool Redelivered => false;

        public Headers TransportHeaders => throw new NotSupportedException();

        public Task ReceiveCompleted => Task.CompletedTask;

        public bool IsDelivered => false;

        public bool IsFaulted => false;

        public ISendEndpointProvider SendEndpointProvider { get; } = sendEndpointProvider;

        public IPublishEndpointProvider PublishEndpointProvider { get; } = publishEndpointProvider;

        public bool PublishFaults => true;

        public MessageBody Body => throw new NotSupportedException();

        public Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType,
            CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception,
            CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void AddReceiveTask(Task task)
        {
        }
    }

    private sealed class RecordingSendEndpointProvider(ISendEndpoint endpoint) : ISendEndpointProvider
    {
        public List<(Uri Address, CancellationToken CancellationToken)> Resolutions { get; } = [];

        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            Resolutions.Add((address, cancellationToken));
            return Task.FromResult(endpoint);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new RecordingConnectHandle();
    }

    private sealed class RecordingPublishEndpointProvider(ISendEndpoint endpoint) : IPublishEndpointProvider
    {
        public List<CancellationToken> Resolutions { get; } = [];

        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            Resolutions.Add(cancellationToken);
            return Task.FromResult(endpoint);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new RecordingConnectHandle();
    }

    private sealed class RecordingSendEndpoint : ISendEndpoint
    {
        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new RecordingConnectHandle();
    }

    private sealed class RecordingConnectHandle : ConnectHandle
    {
        public void Dispose()
        {
        }

        public void Disconnect()
        {
        }
    }

    private class UnsupportedSerializerContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}

[ExcludeFromTopology]
public interface ApplicationCommand;

public interface MemberUpdateCommand : ApplicationCommand
{
    string MemberName { get; }
}

public interface UpdateMemberAddressCommand : MemberUpdateCommand
{
    string Address { get; }
}

public interface BaseFaultCommand
{
    Guid CommandId { get; }
}

public interface DerivedFaultCommand : BaseFaultCommand
{
    string Value { get; }
}
