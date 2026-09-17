using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Mediator.Contexts;
using ViciOne.ServiceBus.Mediator.Runtime;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorEndpointContextDeepContractTests
{
    public static TheoryData<int> SendOverloadForms => [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "addressed-context-identity-metadata-and-body")]
    public async Task AddressedEndpoint_PreservesLogicalIdentityMetadataAndOwnedBodyAsync()
    {
        var captured = new TaskCompletionSource<EndpointSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<EndpointMessage>(context =>
            {
                byte[] body = context.Advanced().ReceiveContext.Body.ToArray();
                using JsonDocument document = JsonDocument.Parse(body);
                captured.TrySetResult(new EndpointSnapshot(
                    context.Message,
                    context.SourceAddress,
                    context.DestinationAddress,
                    context.MessageId,
                    context.CorrelationId,
                    context.ConversationId,
                    context.Headers.Get<string>("tenant"),
                    document.RootElement.GetProperty("value").GetString()));
                return Task.CompletedTask;
            });
        });
        var logicalAddress = new Uri("loopback://localhost/logical-endpoint");
        var endpoint = new AddressedMediatorSendEndpoint(mediator, logicalAddress);
        var message = new EndpointMessage(NewId.NextGuid(), "payload");
        Guid messageId = NewId.NextGuid();

        await endpoint.SendAsync(message, context =>
        {
            context.MessageId = messageId;
            context.Headers.Set("tenant", "north");
        }, TestContext.Current.CancellationToken);

        EndpointSnapshot snapshot = await captured.Task.WaitAsync(TestContext.Current.CancellationToken);
        Assert.Same(mediator, endpoint.Endpoint);
        Assert.Same(message, snapshot.Message);
        Assert.Equal(mediator.Context.ResponseAddress, snapshot.SourceAddress);
        Assert.Equal(logicalAddress, snapshot.DestinationAddress);
        Assert.Equal(messageId, snapshot.MessageId);
        Assert.Equal(message.CorrelationId, snapshot.CorrelationId);
        Assert.NotNull(snapshot.ConversationId);
        Assert.Equal("north", snapshot.Tenant);
        Assert.Equal(message.Value, snapshot.SerializedValue);

        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() =>
            new AddressedMediatorSendEndpoint(mediator, null!)).ParamName);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentException>(() =>
            new AddressedMediatorSendEndpoint(mediator, new Uri("relative", UriKind.Relative))).ParamName);
        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            new AddressedMediatorSendEndpoint(null!, logicalAddress)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ENDPOINT-RESOLUTION", "primary-response-addressed-routes-and-boundaries")]
    public async Task EndpointResolution_PreservesPrimaryResponseAndAddressedIdentityAndBoundariesAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        MediatorSendEndpoint endpoint = GetMediatorEndpoint(mediator);
        var primaryAddress = new Uri("loopback://localhost/mediator");
        Uri responseAddress = mediator.Context.ResponseAddress;
        var logicalAddress = new Uri("loopback://localhost/logical-resolution");

        ISendEndpoint primary = await endpoint.GetSendEndpointAsync(primaryAddress, TestContext.Current.CancellationToken);
        ISendEndpoint response = await endpoint.GetSendEndpointAsync(responseAddress, TestContext.Current.CancellationToken);
        ISendEndpoint addressed = await endpoint.GetSendEndpointAsync(logicalAddress, TestContext.Current.CancellationToken);

        Assert.Same(endpoint, primary);
        Assert.Same(response, await endpoint.GetSendEndpointAsync(responseAddress, TestContext.Current.CancellationToken));
        var responseEndpoint = Assert.IsType<MediatorSendEndpoint>(response);
        var responseAddressed = Assert.IsType<AddressedMediatorSendEndpoint>(
            await responseEndpoint.GetSendEndpointAsync(logicalAddress, TestContext.Current.CancellationToken));
        Assert.Same(responseEndpoint, responseAddressed.Endpoint);
        var addressedProxy = Assert.IsType<AddressedMediatorSendEndpoint>(addressed);
        Assert.Same(endpoint, addressedProxy.Endpoint);
        Type routeProviderType = typeof(ISendEndpoint).Assembly.GetType(
            "ViciOne.ServiceBus.Advanced.IMessageRouteProvider",
            throwOnError: true)!;
        var routes = Assert.IsAssignableFrom<IMessageRouteTable>(
            routeProviderType.GetProperty("MessageRoutes")!.GetValue(endpoint));
        Assert.Same(MessageRouteTable.Empty, routes);
        Assert.Equal("address", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.GetSendEndpointAsync(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentException>(() =>
            endpoint.GetSendEndpointAsync(new Uri("relative", UriKind.Relative), TestContext.Current.CancellationToken))).ParamName);
        using var source = new CancellationTokenSource();
        source.Cancel();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            endpoint.GetSendEndpointAsync(logicalAddress, source.Token));
        Assert.Equal(source.Token, canceled.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-API", "client-context-construction-routing-and-null-boundaries")]
    public async Task ClientFactoryContext_UsesStableDefaultsRoutesEndpointsAndConstructorBoundariesAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        MediatorSendEndpoint endpoint = GetMediatorEndpoint(mediator);
        var existing = Assert.IsType<MediatorClientFactoryContext>(mediator.Context);
        var connector = Assert.IsAssignableFrom<IConsumePipe>(GetField(existing, "_connector"));
        Uri responseAddress = existing.ResponseAddress;
        var defaults = new MediatorClientFactoryContext(endpoint, connector, responseAddress);
        var explicitTimeout = new RequestTimeout(TimeSpan.FromSeconds(7));
        var explicitValues = new MediatorClientFactoryContext(
            endpoint,
            connector,
            responseAddress,
            explicitTimeout,
            TimeProvider.System);

        Assert.Equal(RequestTimeout.Default, defaults.DefaultTimeout);
        Assert.Same(TimeProvider.System, defaults.TimeProvider);
        Assert.Equal(explicitTimeout, explicitValues.DefaultTimeout);
        Assert.Same(TimeProvider.System, explicitValues.TimeProvider);
        Assert.Same(responseAddress, defaults.ResponseAddress);
        Assert.False(defaults.MessageRoutes.TryGetDestinationAddress<EndpointMessage>(out _));
        Assert.IsType<MediatorRequestSendEndpoint<EndpointMessage>>(defaults.GetRequestEndpoint<EndpointMessage>());
        var addressed = Assert.IsType<MediatorRequestSendEndpoint<EndpointMessage>>(
            defaults.GetRequestEndpoint<EndpointMessage>(new Uri("loopback://localhost/client-target")));
        Assert.IsType<AddressedMediatorSendEndpoint>(GetField(addressed, "_endpoint"));

        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            new MediatorClientFactoryContext(null!, connector, responseAddress)).ParamName);
        Assert.Equal("connector", Assert.Throws<ArgumentNullException>(() =>
            new MediatorClientFactoryContext(endpoint, null!, responseAddress)).ParamName);
        Assert.Equal("responseAddress", Assert.Throws<ArgumentNullException>(() =>
            new MediatorClientFactoryContext(endpoint, connector, null!)).ParamName);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() =>
            defaults.GetRequestEndpoint<EndpointMessage>((Uri)null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "context-creation-exact-pipe-cancellation-and-null-task")]
    public async Task CreateSendContext_AppliesTheExactPipeWithoutDispatchAndHonorsBoundariesAsync()
    {
        var handled = 0;
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<EndpointMessage>(_ =>
            {
                Interlocked.Increment(ref handled);
                return Task.CompletedTask;
            });
        });
        MediatorSendEndpoint endpoint = GetMediatorEndpoint(mediator);
        var message = new EndpointMessage(NewId.NextGuid(), "context");
        var pipe = new ContextCreationPipe<EndpointMessage>();
        CancellationToken token = TestContext.Current.CancellationToken;

        SendContext<EndpointMessage> context = await endpoint.CreateSendContextAsync(message, pipe, token);

        Assert.Same(message, context.Message);
        Assert.Same(context, pipe.Context);
        Assert.Equal(token, context.CancellationToken);
        Assert.Equal("created", context.Headers.Get<string>("stage"));
        Assert.Equal(0, Volatile.Read(ref handled));
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.CreateSendContextAsync<EndpointMessage>(null!, pipe, token))).ParamName);
        Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.CreateSendContextAsync(message, null!, token))).ParamName);
        InvalidOperationException nullTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            endpoint.CreateSendContextAsync(message, new BoundarySendPipe<EndpointMessage>(true, new Exception()), token));
        Assert.Equal("The send-context pipe returned no configuration task.", nullTask.Message);
        using var source = new CancellationTokenSource();
        source.Cancel();
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            endpoint.CreateSendContextAsync(message, pipe, source.Token));
        Assert.Equal(source.Token, canceled.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "send-and-publish-probe-forwarding")]
    public async Task InternalSendAndPublishAdapters_ForwardTheExactProbeOnceAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        MediatorSendEndpoint endpoint = GetMediatorEndpoint(mediator);
        var recording = new RecordingProbePipe<EndpointMessage>();
        ProbeContext probe = DispatchProxy.Create<ProbeContext, PassiveProxy>();
        IPipe<SendContext<EndpointMessage>> sendPipe = CreateNestedPipe<MediatorSendEndpoint, EndpointMessage>(
            "MediatorPipe`1", endpoint, recording);
        var publishConfiguration = new PassivePublishPipe();
        IPipe<SendContext<EndpointMessage>> publishPipe = CreateNestedPipe<MediatorPublishSendEndpoint, EndpointMessage>(
            "PublishPipeAdapter`1", publishConfiguration, recording);
        IPipe<SendContext<EndpointMessage>> emptySendPipe = CreateNestedPipe<MediatorSendEndpoint, EndpointMessage>(
            "MediatorPipe`1", endpoint);
        IPipe<SendContext<EndpointMessage>> emptyPublishPipe = CreateNestedPipe<MediatorPublishSendEndpoint, EndpointMessage>(
            "PublishPipeAdapter`1", publishConfiguration, null!);

        sendPipe.Probe(probe);
        publishPipe.Probe(probe);
        emptySendPipe.Probe(probe);
        emptyPublishPipe.Probe(probe);

        Assert.Equal(2, recording.ProbeCount);
        Assert.Same(probe, recording.LastProbe);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ADVANCED-PUBLISH", "endpoint-and-request-constructor-null-boundaries")]
    public async Task PublishAndRequestEndpoints_RejectEveryNullConstructorDependencyAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        var publishPipe = new PassivePublishPipe();
        var observers = new PublishObservable();

        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            new MediatorPublishSendEndpoint(null!, publishPipe, observers)).ParamName);
        Assert.Equal("publishPipe", Assert.Throws<ArgumentNullException>(() =>
            new MediatorPublishSendEndpoint(mediator, null!, observers)).ParamName);
        Assert.Equal("observers", Assert.Throws<ArgumentNullException>(() =>
            new MediatorPublishSendEndpoint(mediator, publishPipe, null!)).ParamName);
        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            new MediatorRequestSendEndpoint<EndpointMessage>(null!, null)).ParamName);

        Type publishAdapter = GetNestedPipeType<MediatorPublishSendEndpoint, EndpointMessage>("PublishPipeAdapter`1");
        ConstructorInfo publishAdapterConstructor = Assert.Single(publishAdapter.GetConstructors());
        TargetInvocationException adapterFailure = Assert.Throws<TargetInvocationException>(() =>
            publishAdapterConstructor.Invoke([null, null]));
        Assert.Equal("publishPipe", Assert.IsType<ArgumentNullException>(adapterFailure.InnerException).ParamName);

        Type mediatorPipe = GetNestedPipeType<MediatorSendEndpoint, EndpointMessage>("MediatorPipe`1");
        ConstructorInfo oneArgument = Assert.Single(mediatorPipe.GetConstructors(), x => x.GetParameters().Length == 1);
        ConstructorInfo twoArguments = Assert.Single(mediatorPipe.GetConstructors(), x => x.GetParameters().Length == 2);
        Assert.Equal("endpoint", ConstructorArgumentNull(oneArgument, [null]).ParamName);
        Assert.Equal("endpoint", ConstructorArgumentNull(twoArguments, null, new CountingSendPipe<EndpointMessage>()).ParamName);
        Assert.Equal("pipe", ConstructorArgumentNull(twoArguments, GetMediatorEndpoint(mediator), null).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "endpoint-constructor-null-collaborators")]
    public void MediatorEndpointConstructor_RejectsEveryNullOwnedCollaborator()
    {
        IReceiveEndpointConfiguration configuration =
            DispatchProxy.Create<IReceiveEndpointConfiguration, ConstructorConfigurationProxy>();
        IReceivePipeDispatcher dispatcher = DispatchProxy.Create<IReceivePipeDispatcher, PassiveProxy>();
        var sendObservers = new SendObservable();
        var publishObservers = new PublishObservable();

        Assert.Equal("configuration", Assert.Throws<ArgumentNullException>(() => new MediatorSendEndpoint(
            null!, dispatcher, null, sendObservers, publishObservers, null!, null!, MessageLimits.Conservative)).ParamName);
        Assert.Equal("dispatcher", Assert.Throws<ArgumentNullException>(() => new MediatorSendEndpoint(
            configuration, null!, null, sendObservers, publishObservers, null!, null!, MessageLimits.Conservative)).ParamName);
        Assert.Equal("sendObservers", Assert.Throws<ArgumentNullException>(() => new MediatorSendEndpoint(
            configuration, dispatcher, null, null!, publishObservers, null!, null!, MessageLimits.Conservative)).ParamName);
        Assert.Equal("publishObservers", Assert.Throws<ArgumentNullException>(() => new MediatorSendEndpoint(
            configuration, dispatcher, null, sendObservers, null!, null!, null!, MessageLimits.Conservative)).ParamName);
        Assert.Equal("messageLimits", Assert.Throws<ArgumentNullException>(() => new MediatorSendEndpoint(
            configuration, dispatcher, null, sendObservers, publishObservers, null!, null!, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "direct-send-overload-null-boundaries")]
    public async Task DirectMediatorSendOverloads_RejectTheirRemainingNullInputsAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        MediatorSendEndpoint endpoint = GetMediatorEndpoint(mediator);
        var message = new EndpointMessage(NewId.NextGuid(), "valid");
        var typedPipe = new CountingSendPipe<EndpointMessage>();
        var untypedPipe = new CountingUntypedSendPipe();
        CancellationToken token = TestContext.Current.CancellationToken;

        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = endpoint.SendAsync<EndpointMessage>(null!, typedPipe, token);
        }).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = endpoint.SendAsync<EndpointMessage>(null!, untypedPipe, token);
        }).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = endpoint.SendAsync((object)null!, untypedPipe, token);
        }).ParamName);
        Assert.Equal("pipe", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = endpoint.SendAsync((object)message, (IPipe<SendContext>)null!, token);
        }).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = endpoint.SendAsync(null!, typeof(EndpointMessage), untypedPipe, token);
        }).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<EndpointMessage>((object)null!, typedPipe, token))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SendAsync<EndpointMessage>((object)null!, untypedPipe, token))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "internal-null-task-and-fallback-destination-contracts")]
    public async Task InternalSendContracts_RejectNullTasksAndUseThePrimaryFallbackDestinationAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(new MessageLimits { MaxBodyBytes = 64, MaxEnvelopeBytes = 64, MaxJsonDepth = 16 }));
        MediatorSendEndpoint endpoint = GetMediatorEndpoint(mediator);
        CancellationToken token = TestContext.Current.CancellationToken;

        InvalidOperationException mediatorPipeFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeSendMessageAsync(endpoint, new EndpointMessage(NewId.NextGuid(), "null-task"),
                new BoundarySendPipe<EndpointMessage>(true, new Exception()), token));
        Assert.Equal("The mediator send pipe returned no configuration task.", mediatorPipeFailure.Message);

        MessageTooLargeException fallbackFailure = await Assert.ThrowsAsync<MessageTooLargeException>(() =>
            InvokeSendMessageAsync(endpoint, new EndpointMessage(NewId.NextGuid(), new string('x', 256)),
                new ContextCreationPipe<EndpointMessage>(), token));
        Assert.Equal(new Uri("loopback://localhost/mediator"), fallbackFailure.EndpointAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "general-endpoint-and-publish-null-task-contracts")]
    public async Task ConfiguredPipes_RejectNullTasksAtTheirExactLayerAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        var message = new EndpointMessage(NewId.NextGuid(), "null-task");
        CancellationToken token = TestContext.Current.CancellationToken;

        InvalidOperationException generalSend = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(message, new GeneralNullSendPipe<EndpointMessage>(), token));
        Assert.Equal("The general send-context pipe returned no configuration task.", generalSend.Message);

        ISendEndpoint publishEndpoint = await mediator.GetPublishSendEndpointAsync<EndpointMessage>(token);
        InvalidOperationException generalPublish = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publishEndpoint.SendAsync(message, new GeneralNullSendPipe<EndpointMessage>(), token));
        Assert.Equal("The general publish send-context pipe returned no configuration task.", generalPublish.Message);

        var nullPublishEndpoint = new MediatorPublishSendEndpoint(mediator, new NullTaskPublishPipe(), new PublishObservable());
        InvalidOperationException publishConfiguration = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullPublishEndpoint.SendAsync(message, token));
        Assert.Equal("The mediator publish pipe returned no configuration task.", publishConfiguration.Message);

        MediatorSendEndpoint endpoint = GetMediatorEndpoint(mediator);
        SetField(endpoint, "_sendPipe", new NullTaskSendPipe());
        InvalidOperationException endpointConfiguration = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(message, token));
        Assert.Equal("The mediator endpoint send pipe returned no configuration task.", endpointConfiguration.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ADVANCED-SEND", "general-and-typed-layers-once-with-caller-token")]
    public async Task SendPipe_AppliesGeneralAndTypedLayersOnceInOrderWithTheCallerTokenAsync()
    {
        EndpointSnapshot? captured = null;
        await using IMediator mediator = CreateCapturingMediator(snapshot => captured = snapshot);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var pipe = new LayeredSendPipe<EndpointMessage>();
        var message = new EndpointMessage(NewId.NextGuid(), "send-layers");

        await mediator.SendAsync(message, pipe, source.Token);

        Assert.Equal(["general", "typed"], pipe.Events);
        Assert.Equal(1, pipe.GeneralCount);
        Assert.Equal(1, pipe.TypedCount);
        Assert.Equal(source.Token, pipe.GeneralCancellationToken);
        Assert.NotNull(captured);
        Assert.Same(message, captured.Message);
        Assert.Equal("general", captured.GeneralHeader);
        Assert.Equal("typed", captured.TypedHeader);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-ADVANCED-PUBLISH", "general-and-typed-layers-once-with-caller-token")]
    public async Task PublishPipe_AppliesGeneralAndTypedLayersOnceInOrderWithTheCallerTokenAsync()
    {
        EndpointSnapshot? captured = null;
        await using IMediator mediator = CreateCapturingMediator(snapshot => captured = snapshot);
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var pipe = new LayeredSendPipe<EndpointMessage>();
        var message = new EndpointMessage(NewId.NextGuid(), "publish-layers");
        ISendEndpoint endpoint = await mediator.GetPublishSendEndpointAsync<EndpointMessage>(source.Token);

        await endpoint.SendAsync(message, pipe, source.Token);

        Assert.Equal(["general", "typed"], pipe.Events);
        Assert.Equal(1, pipe.GeneralCount);
        Assert.Equal(1, pipe.TypedCount);
        Assert.Equal(source.Token, pipe.GeneralCancellationToken);
        Assert.NotNull(captured);
        Assert.Same(message, captured.Message);
        Assert.True(captured.IsPublish);
        Assert.Equal("general", captured.GeneralHeader);
        Assert.Equal("typed", captured.TypedHeader);
    }

    [Theory]
    [MemberData(nameof(SendOverloadForms))]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "every-send-overload-pre-cancellation")]
    public async Task PreCanceledSendOverload_DoesNotInvokeCallerPipesOrHandlersAsync(int form)
    {
        var handled = 0;
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<EndpointMessage>(_ =>
            {
                Interlocked.Increment(ref handled);
                return Task.CompletedTask;
            });
        });
        var typedPipe = new CountingSendPipe<EndpointMessage>();
        var untypedPipe = new CountingUntypedSendPipe();
        IAdvancedSendEndpoint advanced = ((ISendEndpoint)mediator).Advanced();
        var message = new EndpointMessage(NewId.NextGuid(), "canceled");
        object values = new { CorrelationId = message.CorrelationId, Value = message.Value };
        using var source = new CancellationTokenSource();
        source.Cancel();

        Task Invoke() => form switch
        {
            0 => mediator.SendAsync(message, source.Token),
            1 => mediator.SendAsync(message, typedPipe, source.Token),
            2 => mediator.SendAsync(message, untypedPipe, source.Token),
            3 => advanced.SendAsync((object)message, source.Token),
            4 => advanced.SendAsync(message, typeof(EndpointMessage), source.Token),
            5 => advanced.SendAsync((object)message, untypedPipe, source.Token),
            6 => advanced.SendAsync(message, typeof(EndpointMessage), untypedPipe, source.Token),
            7 => advanced.SendAsync<EndpointMessage>(values, source.Token),
            8 => advanced.SendAsync<EndpointMessage>(values, typedPipe, source.Token),
            9 => advanced.SendAsync<EndpointMessage>(values, untypedPipe, source.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(form), form, null),
        };

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Invoke());

        Assert.Equal(source.Token, failure.CancellationToken);
        Assert.Equal(0, typedPipe.Count);
        Assert.Equal(0, untypedPipe.Count);
        Assert.Equal(0, Volatile.Read(ref handled));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "send-and-publish-pipe-exception-versus-null-task")]
    public async Task CallerPipe_DistinguishesItsExactExceptionFromANullTaskAsync(bool publish, bool returnsNullTask)
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        var expected = new ApplicationException("caller pipe failed");
        var message = new EndpointMessage(NewId.NextGuid(), returnsNullTask ? "null" : "exception");
        Task Action() => publish
            ? mediator.PublishAsync(message, new BoundaryPublishPipe<EndpointMessage>(returnsNullTask, expected),
                TestContext.Current.CancellationToken)
            : mediator.SendAsync(message, new BoundarySendPipe<EndpointMessage>(returnsNullTask, expected),
                TestContext.Current.CancellationToken);

        Exception failure = await Assert.ThrowsAnyAsync<Exception>(Action);

        if (returnsNullTask)
        {
            InvalidOperationException contractFailure = Assert.IsType<InvalidOperationException>(failure);
            Assert.Equal(
                publish
                    ? "The additional publish send pipe returned no configuration task."
                    : "The additional mediator send pipe returned no configuration task.",
                contractFailure.Message);
        }
        else
            Assert.Same(expected, failure);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SEND-OBSERVER", "send-and-publish-observer-exception-versus-null-task")]
    public async Task Observer_DistinguishesItsExactExceptionFromANullTaskAsync(bool publish, bool returnsNullTask)
    {
        var handled = 0;
        await using IMediator mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<EndpointMessage>(_ =>
            {
                Interlocked.Increment(ref handled);
                return Task.CompletedTask;
            });
        });
        var expected = new ApplicationException("observer failed");
        var sendObserver = new BoundarySendObserver(returnsNullTask, expected);
        var publishObserver = new BoundaryPublishObserver(returnsNullTask, expected);
        using ConnectHandle handle = publish
            ? mediator.ConnectPublishObserver(publishObserver)
            : mediator.ConnectSendObserver(sendObserver);
        var message = new EndpointMessage(NewId.NextGuid(), returnsNullTask ? "null" : "exception");
        Task Action() => publish
            ? mediator.PublishAsync(message, TestContext.Current.CancellationToken)
            : mediator.SendAsync(message, TestContext.Current.CancellationToken);

        Exception failure = await Assert.ThrowsAnyAsync<Exception>(Action);

        if (returnsNullTask)
        {
            InvalidOperationException contractFailure = Assert.IsType<InvalidOperationException>(failure);
            Assert.Equal("The connection callback returned a null task.", contractFailure.Message);
        }
        else
            Assert.Same(expected, failure);

        Assert.Same(failure, publish ? publishObserver.FaultException : sendObserver.FaultException);
        Assert.Equal(0, Volatile.Read(ref handled));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-BODY", "pre-cancellation-before-serialization")]
    public async Task Serializer_PreCancellationPreservesTheExactTokenAsync()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            MediatorMessageBodySerializer.SerializeAsync(
                new EndpointMessage(NewId.NextGuid(), "body"),
                new JsonSerializerOptions(ServiceBusMetadataJson.Options),
                MessageLimits.Conservative,
                new Uri("loopback://localhost/mediator"),
                source.Token));

        Assert.Equal(source.Token, failure.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-BODY-STREAM", "nonzero-capacity-growth-retains-content")]
    public void BoundedStream_GrowsFromAnExistingCapacityWithoutLosingContent()
    {
        using var stream = new MediatorMessageBodySerializer.BoundedMessageBodyStream(
            600,
            new Uri("loopback://localhost/growth"));
        byte[] prefix = Enumerable.Repeat((byte)0x2a, 256).ToArray();

        stream.Write(prefix);
        stream.WriteByte(0x7f);
        MessageBody body = stream.Complete();

        Assert.Equal(257, body.Length);
        byte[] content = body.ToArray();
        Assert.Equal(prefix, content[..256]);
        Assert.Equal((byte)0x7f, content[256]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-SEND-OBSERVER", "fault-observer-failure-uses-enabled-error-log")]
    public async Task FaultObserverFailure_IsLoggedWithoutReplacingTheDispatchFailureAsync()
    {
        ILogContext? previous = LogContext.Current;
        var logger = new RecordingEnabledLogger();
        var dispatchFailure = new InvalidOperationException("dispatch failed");
        var observerFailure = new ApplicationException("fault observer failed");

        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            await using IMediator mediator = MediatorFactory.Create(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.Handler<EndpointMessage>(_ => Task.FromException(dispatchFailure));
            });
            using ConnectHandle handle = mediator.ConnectSendObserver(new FaultingSendObserver(observerFailure));

            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                mediator.SendAsync(new EndpointMessage(NewId.NextGuid(), "log"), TestContext.Current.CancellationToken));

            Assert.Same(dispatchFailure, failure);
            Assert.Same(observerFailure, logger.Exception);
            Assert.Contains("mediator send-fault observer failed", logger.Message, StringComparison.Ordinal);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "dispatcher-null-task-is-explicit-and-observed")]
    public async Task Dispatcher_NullTaskBecomesAnExplicitObservedDispatchFailureAsync()
    {
        var observer = new RecordingFaultObserver();
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        using ConnectHandle handle = mediator.ConnectSendObserver(observer);
        var runtime = Assert.IsType<InProcessMediator>(mediator);
        MediatorSendEndpoint endpoint = Assert.IsType<MediatorSendEndpoint>(GetField(runtime, "_endpoint"));
        SetField(endpoint, "_dispatcher", DispatchProxy.Create<IReceivePipeDispatcher, NullTaskDispatcherProxy>());

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.SendAsync(
            new EndpointMessage(NewId.NextGuid(), "dispatch"),
            TestContext.Current.CancellationToken));

        Assert.Equal("The mediator receive dispatcher returned no dispatch task.", failure.Message);
        Assert.Same(failure, observer.FaultException);
    }

    private static IMediator CreateCapturingMediator(Action<EndpointSnapshot> capture) =>
        MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<EndpointMessage>(context =>
            {
                capture(new EndpointSnapshot(
                    context.Message,
                    context.SourceAddress,
                    context.DestinationAddress,
                    context.MessageId,
                    context.CorrelationId,
                    context.ConversationId,
                    null,
                    null,
                    context.Headers.Get<string>("general"),
                    context.Headers.Get<string>("typed"),
                    context.Advanced().ReceiveContext.IsDelivered));
                return Task.CompletedTask;
            });
        });

    private static MediatorSendEndpoint GetMediatorEndpoint(IMediator mediator)
    {
        var runtime = Assert.IsType<InProcessMediator>(mediator);
        return Assert.IsType<MediatorSendEndpoint>(GetField(runtime, "_endpoint"));
    }

    private static Type GetNestedPipeType<TOwner, TMessage>(string name)
        where TMessage : class
    {
        Type? openType = typeof(TOwner).GetNestedType(name, BindingFlags.NonPublic);
        Assert.NotNull(openType);
        return openType.MakeGenericType(typeof(TMessage));
    }

    private static ArgumentNullException ConstructorArgumentNull(ConstructorInfo constructor, params object?[] arguments)
    {
        TargetInvocationException failure = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(arguments));
        return Assert.IsType<ArgumentNullException>(failure.InnerException);
    }

    private static Task InvokeSendMessageAsync<T>(
        MediatorSendEndpoint endpoint,
        T message,
        IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        MethodInfo? openMethod = typeof(MediatorSendEndpoint).GetMethod(
            "SendMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(openMethod);
        MethodInfo method = openMethod.MakeGenericMethod(typeof(T));
        return Assert.IsAssignableFrom<Task>(method.Invoke(endpoint, [message, pipe, cancellationToken]));
    }

    private static IPipe<SendContext<TMessage>> CreateNestedPipe<TOwner, TMessage>(string name, params object?[] arguments)
        where TMessage : class
    {
        Type closedType = GetNestedPipeType<TOwner, TMessage>(name);
        return Assert.IsAssignableFrom<IPipe<SendContext<TMessage>>>(Activator.CreateInstance(closedType, arguments));
    }

    private static object GetField(object instance, string name) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static void SetField(object instance, string name, object value) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);

    private sealed class LayeredSendPipe<T> : IPipe<SendContext<T>>, ISendContextPipe
        where T : class
    {
        private int _generalCount;
        private int _typedCount;

        public List<string> Events { get; } = [];
        public int GeneralCount => Volatile.Read(ref _generalCount);
        public int TypedCount => Volatile.Read(ref _typedCount);
        public CancellationToken GeneralCancellationToken { get; private set; }

        public Task SendAsync(SendContext<T> context)
        {
            Events.Add("typed");
            context.Headers.Set("typed", "typed");
            Interlocked.Increment(ref _typedCount);
            return Task.CompletedTask;
        }

        Task ISendContextPipe.SendAsync<TMessage>(SendContext<TMessage> context, CancellationToken cancellationToken)
        {
            Events.Add("general");
            GeneralCancellationToken = cancellationToken;
            context.Headers.Set("general", "general");
            Interlocked.Increment(ref _generalCount);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateScope("layered");
    }

    private sealed class GeneralNullSendPipe<T> : IPipe<SendContext<T>>, ISendContextPipe
        where T : class
    {
        public Task SendAsync(SendContext<T> context) => Task.CompletedTask;

        Task ISendContextPipe.SendAsync<TMessage>(SendContext<TMessage> context, CancellationToken cancellationToken) => null!;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class NullTaskPublishPipe : IPublishPipe
    {
        public Task SendAsync<T>(PublishContext<T> context, CancellationToken cancellationToken = default)
            where T : class => null!;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class NullTaskSendPipe : ISendPipe
    {
        public Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
            where T : class => null!;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class CountingSendPipe<T> : IPipe<SendContext<T>>
        where T : class
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public Task SendAsync(SendContext<T> context)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateScope("counting");
    }

    private sealed class CountingUntypedSendPipe : IPipe<SendContext>
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public Task SendAsync(SendContext context)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateScope("countingUntyped");
    }

    private sealed class ContextCreationPipe<T> : IPipe<SendContext<T>>
        where T : class
    {
        public SendContext<T>? Context { get; private set; }

        public Task SendAsync(SendContext<T> context)
        {
            Context = context;
            context.Headers.Set("stage", "created");
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateScope("contextCreation");
    }

    private sealed class RecordingProbePipe<T> : IPipe<SendContext<T>>
        where T : class
    {
        private int _probeCount;

        public ProbeContext? LastProbe { get; private set; }
        public int ProbeCount => Volatile.Read(ref _probeCount);

        public Task SendAsync(SendContext<T> context) => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
            LastProbe = context;
            Interlocked.Increment(ref _probeCount);
        }
    }

    private sealed class PassivePublishPipe : IPublishPipe
    {
        public Task SendAsync<T>(PublishContext<T> context, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class BoundarySendPipe<T>(bool returnsNullTask, Exception failure) : IPipe<SendContext<T>>
        where T : class
    {
        public Task SendAsync(SendContext<T> context)
        {
            if (returnsNullTask)
                return null!;

            throw failure;
        }

        public void Probe(ProbeContext context) => context.CreateScope("boundary");
    }

    private sealed class BoundaryPublishPipe<T>(bool returnsNullTask, Exception failure) : IPipe<PublishContext<T>>
        where T : class
    {
        public Task SendAsync(PublishContext<T> context)
        {
            if (returnsNullTask)
                return null!;

            throw failure;
        }

        public void Probe(ProbeContext context) => context.CreateScope("boundaryPublish");
    }

    private sealed class BoundarySendObserver(bool returnsNullTask, Exception failure) : ISendObserver
    {
        public Exception? FaultException { get; private set; }

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (returnsNullTask)
                return null!;

            throw failure;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            FaultException = exception;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingFaultObserver : ISendObserver
    {
        public Exception? FaultException { get; private set; }

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            FaultException = exception;
            return Task.CompletedTask;
        }
    }

    private sealed class FaultingSendObserver(Exception failure) : ISendObserver
    {
        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class => Task.FromException(failure);
    }

    private sealed class RecordingEnabledLogger : ILogger
    {
        public Exception? Exception { get; private set; }
        public string Message { get; private set; } = string.Empty;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Exception = exception;
            Message = formatter(state, exception);
        }
    }

    private sealed class BoundaryPublishObserver(bool returnsNullTask, Exception failure) : IPublishObserver
    {
        public Exception? FaultException { get; private set; }

        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            if (returnsNullTask)
                return null!;

            throw failure;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class
        {
            FaultException = exception;
            return Task.CompletedTask;
        }
    }

    public class NullTaskDispatcherProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IReceivePipeDispatcher.DispatchAsync))
                return null;

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    public class ConstructorConfigurationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_InputAddress" => new Uri("loopback://localhost/constructor"),
            "get_Topology" => DispatchProxy.Create<ITopologyConfiguration, ConstructorTopologyProxy>(),
            "get_ReceiveObservers" => new ReceiveObservable(),
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    public class ConstructorTopologyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_Publish" => DispatchProxy.Create<IPublishTopologyConfigurator, PassiveProxy>(),
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }

    public class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed record EndpointMessage(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    private sealed record EndpointSnapshot(
        EndpointMessage Message,
        Uri? SourceAddress,
        Uri? DestinationAddress,
        Guid? MessageId,
        Guid? CorrelationId,
        Guid? ConversationId,
        string? Tenant,
        string? SerializedValue,
        string? GeneralHeader = null,
        string? TypedHeader = null,
        bool IsPublish = false);
}
