using ViciOne.ServiceBus.Consumers;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class InMemoryTestHarnessBehaviorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "send-consume-respond-observations")]
    public async Task ConsumerHarness_ObservesTheCompleteSendConsumeAndPublishPathAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<ReplyingConsumer> consumer = harness.AddConsumer<ReplyingConsumer>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new RequestMessage("request"), cancellationToken);

            Assert.True(await harness.Sent.AnyAsync<RequestMessage>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<RequestMessage>(cancellationToken));
            Assert.True(await consumer.Consumed.AnyAsync<RequestMessage>(cancellationToken));

            IPublishedMessage<ReplyMessage> response = await harness.Published
                .SelectAsync<ReplyMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("reply:request", response.Context.Message.Value);
            Assert.Null(response.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "in-flight-completion-without-wall-clock-delay")]
    public async Task ConsumerHarness_DoesNotReportCompletionBeforeTheConsumerCompletesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<GatedConsumer> consumer = harness.AddConsumer(() => new GatedConsumer(entered, release));

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new GatedMessage(), cancellationToken);
            await entered.Task.WaitAsync(timeout, cancellationToken);

            Task<bool> observation = consumer.Consumed.AnyAsync<GatedMessage>(cancellationToken);
            Assert.False(observation.IsCompleted);

            release.TrySetResult(true);

            Assert.True(await observation.WaitAsync(timeout, cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<GatedMessage>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<GatedMessage>(cancellationToken));
            IPublishedMessage<GatedResponse> response = await harness.Published
                .SelectAsync<GatedResponse>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("completed", response.Context.Message.Value);
            Assert.Null(response.Exception);
        }
        finally
        {
            release.TrySetResult(true);
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "multiple-message-contracts-and-addressed-responses")]
    public async Task MultiContractConsumer_RecordsBothContractsAndTheirAddressedResponsesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<MultiContractConsumer> consumer = harness.AddConsumer<MultiContractConsumer>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                new FirstRequest(),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(
                new SecondRequest(),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);

            Assert.True(await consumer.Consumed.AnyAsync<FirstRequest>(cancellationToken));
            Assert.True(await consumer.Consumed.AnyAsync<SecondRequest>(cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<FirstRequest>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<FirstRequest>(cancellationToken));

            ISentMessage<FirstResponse> first = await harness.Sent.SelectAsync<FirstResponse>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            ISentMessage<SecondResponse> second = await harness.Sent.SelectAsync<SecondResponse>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(harness.BusAddress, first.Context.DestinationAddress);
            Assert.Equal(harness.BusAddress, second.Context.DestinationAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "interface-contract-projection")]
    public async Task InterfaceConsumer_PreservesConcreteAndInterfaceObservationsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<InterfaceConsumer> consumer = harness.AddConsumer<InterfaceConsumer>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new ConcreteRequest("value"), cancellationToken);

            Assert.True(await harness.Sent.AnyAsync<ConcreteRequest>(cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<IRequestContract>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<IRequestContract>(cancellationToken));
            Assert.True(await consumer.Consumed.AnyAsync<IRequestContract>(cancellationToken));
            Assert.True(await harness.Published.AnyAsync<ConcreteReply>(cancellationToken));
            Assert.True(await harness.Published.AnyAsync<IReplyContract>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "response-destination-and-observation")]
    public async Task HandlerHarness_RecordsTheHandledMessageAndExactResponseDestinationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<HandlerRequest> handler = harness.AddHandler<HandlerRequest>(context =>
            context.RespondAsync(new HandlerResponse(context.Message.Value + 1)));

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                new HandlerRequest(41),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);

            IConsumedMessage<HandlerRequest> handled = await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            ISentMessage<HandlerResponse> response = await harness.Sent.SelectAsync<HandlerResponse>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(41, handled.Context.Message.Value);
            Assert.Null(handled.Exception);
            Assert.Equal(42, response.Context.Message.Value);
            Assert.Equal(harness.BusAddress, response.Context.DestinationAddress);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "failure-recorded-and-propagated")]
    public async Task HandlerHarness_RecordsAndPropagatesTheExactHandlerFailureAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected handler failure");
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<FailingHandlerMessage> handler = harness.AddHandler<FailingHandlerMessage>(_ => Task.FromException(expected));

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new FailingHandlerMessage(), cancellationToken);

            IConsumedMessage<FailingHandlerMessage> handled = await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IConsumedMessage<FailingHandlerMessage> pipeline = await harness.Consumed
                .SelectAsync<FailingHandlerMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Same(expected, handled.Exception);
            Assert.Same(expected, pipeline.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "default-handler-send-observation")]
    public async Task DefaultHandlerHarness_ObservesASentMessageWithoutApplicationBehaviorAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<PassiveHandlerMessage> handler = harness.AddHandler<PassiveHandlerMessage>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new PassiveHandlerMessage("sent"), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new UnconsumedPassiveMessage("also-sent"), cancellationToken);

            IConsumedMessage<PassiveHandlerMessage> handled = await handler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("sent", handled.Context.Message.Value);
            Assert.Null(handled.Exception);
            Assert.True(await harness.Sent.AnyAsync<PassiveHandlerMessage>(cancellationToken));
            Assert.True(await harness.Sent.AnyAsync<UnconsumedPassiveMessage>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<PassiveHandlerMessage>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "default-handler-publish-interface-projection")]
    public async Task DefaultHandlerHarness_ObservesPublishedConcreteAndInterfaceContractsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        HandlerTestHarness<IPassiveHandlerContract> handler = harness.AddHandler<IPassiveHandlerContract>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.Bus.PublishAsync(new ConcretePassiveHandlerMessage("published"), cancellationToken);

            IConsumedMessage<IPassiveHandlerContract> handled = await handler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("published", handled.Context.Message.Value);
            Assert.Null(handled.Exception);
            Assert.True(await harness.Published.AnyAsync<ConcretePassiveHandlerMessage>(cancellationToken));
            Assert.True(await harness.Published.AnyAsync<IPassiveHandlerContract>(cancellationToken));
            Assert.True(await harness.Consumed.AnyAsync<IPassiveHandlerContract>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "failure-recorded-and-propagated")]
    public async Task ConsumerHarness_RecordsAndPropagatesTheExactConsumerFailureAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected consumer failure");
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<FailingConsumer> consumer = harness.AddConsumer(() => new FailingConsumer(expected));

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new FailingConsumerMessage(), cancellationToken);

            IConsumedMessage<FailingConsumerMessage> consumerObservation = await consumer.Consumed
                .SelectAsync<FailingConsumerMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IConsumedMessage<FailingConsumerMessage> pipelineObservation = await harness.Consumed
                .SelectAsync<FailingConsumerMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Same(expected, consumerObservation.Exception);
            Assert.Same(expected, pipelineObservation.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "named-endpoint-configuration")]
    public async Task NamedConsumerHarness_ConsumesOnlyFromItsDedicatedEndpointAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string queueName = "dedicated-consumer";
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<NamedConsumer> consumer = harness.AddConsumer<NamedConsumer>(queueName);

        await harness.StartAsync(cancellationToken);
        try
        {
            ISendEndpoint endpoint = await harness.GetSendEndpointAsync(new Uri(harness.BaseAddress, queueName), TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new NamedConsumerMessage("expected"), cancellationToken);

            IConsumedMessage<NamedConsumerMessage> received = await consumer.Consumed
                .SelectAsync<NamedConsumerMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("expected", received.Context.Message.Value);
            Assert.Equal(new Uri(harness.BaseAddress, queueName), received.Context.Advanced().ReceiveContext.InputAddress);
            Assert.Null(received.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "all-factory-and-configuration-overloads")]
    public async Task ConsumerRegistration_AllFactoryAndConfigurationOverloadsDeliverThroughTheirOwnEndpointsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var configurationCount = 0;
        var directConfigurationCount = 0;
        using var harness = CreateHarness(timeout);
        ConsumerTestHarness<NamedConsumer> defaultConfigured = harness.AddConsumer<NamedConsumer>(
            _ => Interlocked.Increment(ref configurationCount),
            "default-configured");
        ConsumerTestHarness<NamedConsumer> factory = harness.AddConsumer<NamedConsumer>(
            new DelegateConsumerFactory<NamedConsumer>(() => new NamedConsumer()),
            "factory");
        ConsumerTestHarness<NamedConsumer> factoryConfigured = harness.AddConsumer<NamedConsumer>(
            new DelegateConsumerFactory<NamedConsumer>(() => new NamedConsumer()),
            _ => Interlocked.Increment(ref configurationCount),
            "factory-configured");
        ConsumerTestHarness<NamedConsumer> delegateConfigured = harness.AddConsumer(
            () => new NamedConsumer(),
            _ => Interlocked.Increment(ref configurationCount),
            "delegate-configured");
        var directConfigured = new ConsumerTestHarness<NamedConsumer>(
            harness,
            new DelegateConsumerFactory<NamedConsumer>(() => new NamedConsumer()),
            _ => Interlocked.Increment(ref directConfigurationCount));

        await harness.StartAsync(cancellationToken);
        try
        {
            Assert.Equal(3, Volatile.Read(ref configurationCount));
            Assert.Equal(1, Volatile.Read(ref directConfigurationCount));

            await SendAndAssertConsumedAsync(defaultConfigured, "default-configured", harness, cancellationToken);
            await SendAndAssertConsumedAsync(factory, "factory", harness, cancellationToken);
            await SendAndAssertConsumedAsync(factoryConfigured, "factory-configured", harness, cancellationToken);
            await SendAndAssertConsumedAsync(delegateConfigured, "delegate-configured", harness, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new NamedConsumerMessage("direct-configured"), cancellationToken);
            IConsumedMessage<NamedConsumerMessage> directlyReceived = await directConfigured.Consumed
                .SelectAsync<NamedConsumerMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: cancellationToken);
            Assert.Equal("direct-configured", directlyReceived.Context.Message.Value);
            Assert.Equal(harness.InputQueueAddress, directlyReceived.Context.Advanced().ReceiveContext.InputAddress);
            Assert.Null(directlyReceived.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONSUMER", "registration-boundaries")]
    public void ConsumerRegistration_RejectsEveryAbsentDelegateFactoryAndConfiguration()
    {
        using var harness = CreateHarness(OperationTimeout());
        var factory = new DelegateConsumerFactory<NamedConsumer>(() => new NamedConsumer());

        Assert.Equal("harness", Assert.Throws<ArgumentNullException>(() =>
            ConsumerTestHarnessExtensions.AddConsumer<NamedConsumer>(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            harness.AddConsumer<NamedConsumer>((Action<IConsumerConfigurator<NamedConsumer>>)null!)).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
            harness.AddConsumer<NamedConsumer>((IConsumerFactory<NamedConsumer>)null!)).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
            harness.AddConsumer<NamedConsumer>((IConsumerFactory<NamedConsumer>)null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            harness.AddConsumer(factory, (Action<IConsumerConfigurator<NamedConsumer>>)null!)).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
            harness.AddConsumer<NamedConsumer>((Func<NamedConsumer>)null!)).ParamName);
        Assert.Equal("consumerFactory", Assert.Throws<ArgumentNullException>(() =>
            harness.AddConsumer<NamedConsumer>((Func<NamedConsumer>)null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            harness.AddConsumer(() => new NamedConsumer(), (Action<IConsumerConfigurator<NamedConsumer>>)null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "public-boundaries-and-repeated-start")]
    public async Task PublicBoundaries_RejectInvalidInputsAndRepeatedStartAsync()
    {
        Assert.Equal("virtualHost", Assert.Throws<ArgumentException>(() => new InMemoryTestHarness(" ")).ParamName);
        Assert.Equal("virtualHost", Assert.Throws<ArgumentException>(() => new InMemoryTestHarness("///")).ParamName);

        TimeSpan timeout = OperationTimeout();
        using var harness = CreateHarness(timeout);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => harness.ConnectConsumeObserver(null!)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => harness.ConnectPublishObserver(null!)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => harness.ConnectSendObserver(null!)).ParamName);
        Assert.Equal("destinationAddress", Assert.Throws<ArgumentNullException>(() =>
            harness.CreateRequestClient<RequestMessage>(null!)).ParamName);
        Assert.Equal("address", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = harness.GetSendEndpointAsync(null!, TestContext.Current.CancellationToken);
        }).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
        {
            _ = harness.WaitForMessageAsync<RequestMessage>(null!, TestContext.Current.CancellationToken);
        }).ParamName);

        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                harness.StartAsync(TestContext.Current.CancellationToken));
            Assert.Contains("already been started", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }

        Assert.Throws<InvalidOperationException>(() => harness.BusControl);
        Assert.DoesNotContain(
            typeof(InMemoryTestHarness).GetMethods(),
            method => method.Name == "ConnectRequestClientAsync");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "delegate-failure-completes-awaitable")]
    public async Task WaitForHandlerExecutionAsync_PropagatesTheExactDelegateFailureAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected handler failure");
        Task<ConsumeContext<FailingHandlerMessage>>? handled = null;
        using var harness = CreateHarness(timeout);
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            handled = harness.WaitForHandlerExecutionAsync<FailingHandlerMessage>(configurator, _ => Task.FromException(expected));

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new FailingHandlerMessage(), cancellationToken);

            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Assert.IsType<Task<ConsumeContext<FailingHandlerMessage>>>(handled).WaitAsync(timeout, cancellationToken));
            Assert.Same(expected, actual);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "failed-start-rolls-back-created-bus")]
    public async Task ObserverConnectionFailure_StopsTheCreatedBusAndClearsOwnedStateAsync()
    {
        TimeSpan timeout = OperationTimeout();
        using var harness = CreateHarness(timeout);
        var expected = new InvalidOperationException("observer connection failed");

        void FailObserverConnection(IBus _) => throw expected;

        harness.ObserversConnecting += FailObserverConnection;
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.StartAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Throws<InvalidOperationException>(() => harness.BusControl);
        Assert.Throws<InvalidOperationException>(() => harness.BusSendEndpoint);
        Assert.Throws<InvalidOperationException>(() => harness.InputQueueSendEndpoint);

        harness.ObserversConnecting -= FailObserverConnection;
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "wait-api-success-overloads")]
    public async Task WaitApis_CompleteForPlainFilteredCountedDelegateAndConsumerRegistrationsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Task<ConsumeContext<PlainWaitMessage>>? plain = null;
        Task<ConsumeContext<FilteredWaitMessage>>? filtered = null;
        Task<ConsumeContext<CountedWaitMessage>>? counted = null;
        Task<ConsumeContext<DelegateWaitMessage>>? delegated = null;
        Task<ConsumeContext<ConsumerWaitMessage>>? consumed = null;
        using var harness = CreateHarness(timeout);
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
        {
            plain = harness.WaitForHandledMessageAsync<PlainWaitMessage>(configurator, cancellationToken);
            filtered = harness.WaitForHandledMessageAsync<FilteredWaitMessage>(
                configurator,
                context => context.Message.Accept,
                cancellationToken);
            counted = harness.WaitForHandledMessageAsync<CountedWaitMessage>(configurator, 2, cancellationToken);
            delegated = harness.WaitForHandlerExecutionAsync<DelegateWaitMessage>(
                configurator,
                context => context.RespondAsync(new DelegateWaitResponse(context.Message.Value + 1)),
                cancellationToken);
            consumed = harness.WaitForConsumerAsync<ConsumerWaitMessage>(configurator, cancellationToken);
        };

        await harness.StartAsync(cancellationToken);
        try
        {
            Task<ConsumeContext<SubscriptionWaitMessage>> subscription = harness.WaitForMessageAsync<SubscriptionWaitMessage>(
                context => context.Message.Accept,
                cancellationToken);
            await harness.BusSendEndpoint.SendAsync(new SubscriptionWaitMessage("ignored", false), cancellationToken);
            await harness.BusSendEndpoint.SendAsync(new SubscriptionWaitMessage("accepted", true), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new PlainWaitMessage("plain"), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new FilteredWaitMessage("ignored", false), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new FilteredWaitMessage("accepted", true), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new CountedWaitMessage(1), cancellationToken);
            Assert.True(await harness.Consumed.AnyAsync<CountedWaitMessage>(
                message => message.Context.Message.Value == 1,
                cancellationToken));
            await harness.InputQueueSendEndpoint.SendAsync(new CountedWaitMessage(2), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(
                new DelegateWaitMessage(41),
                context => context.ResponseAddress = harness.BusAddress,
                cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new ConsumerWaitMessage("consumer"), cancellationToken);

            Assert.Equal("plain", (await Assert.IsType<Task<ConsumeContext<PlainWaitMessage>>>(plain)
                .WaitAsync(timeout, cancellationToken)).Message.Value);
            Assert.Equal("accepted", (await Assert.IsType<Task<ConsumeContext<FilteredWaitMessage>>>(filtered)
                .WaitAsync(timeout, cancellationToken)).Message.Value);
            Assert.Equal(2, (await Assert.IsType<Task<ConsumeContext<CountedWaitMessage>>>(counted)
                .WaitAsync(timeout, cancellationToken)).Message.Value);
            Assert.Equal(41, (await Assert.IsType<Task<ConsumeContext<DelegateWaitMessage>>>(delegated)
                .WaitAsync(timeout, cancellationToken)).Message.Value);
            Assert.Equal("consumer", (await Assert.IsType<Task<ConsumeContext<ConsumerWaitMessage>>>(consumed)
                .WaitAsync(timeout, cancellationToken)).Message.Value);
            Assert.Equal("accepted", (await subscription.WaitAsync(timeout, cancellationToken)).Message.Value);
            Assert.True(await harness.Sent.AnyAsync<DelegateWaitResponse>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-HANDLER", "wait-api-boundaries")]
    public async Task WaitApis_RejectEveryInvalidArgumentAndPreservePreCanceledTokensAsync()
    {
        TimeSpan timeout = OperationTimeout();
        using var harness = CreateHarness(timeout);
        IReceiveEndpointConfigurator? configurator = null;
        harness.InMemoryReceiveEndpointConfiguring += endpoint => configurator = endpoint;
        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            IReceiveEndpointConfigurator endpoint = Assert.IsAssignableFrom<IReceiveEndpointConfigurator>(configurator);
            Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            {
                _ = harness.WaitForHandledMessageAsync<PlainWaitMessage>(null!, TestContext.Current.CancellationToken);
            }).ParamName);
            Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
            {
                _ = harness.WaitForHandledMessageAsync<FilteredWaitMessage>(endpoint, null!, TestContext.Current.CancellationToken);
            }).ParamName);
            Assert.Equal("expectedCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = harness.WaitForHandledMessageAsync<CountedWaitMessage>(endpoint, 0, TestContext.Current.CancellationToken);
            }).ParamName);
            Assert.Equal("handler", Assert.Throws<ArgumentNullException>(() =>
            {
                _ = harness.WaitForHandlerExecutionAsync<DelegateWaitMessage>(endpoint, null!, TestContext.Current.CancellationToken);
            }).ParamName);
            Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            {
                _ = harness.WaitForConsumerAsync<ConsumerWaitMessage>(null!, TestContext.Current.CancellationToken);
            }).ParamName);

            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            Task<ConsumeContext<PlainWaitMessage>> wait = harness.WaitForHandledMessageAsync<PlainWaitMessage>(endpoint, canceled.Token);
            OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
            Assert.Equal(canceled.Token, exception.CancellationToken);

            await harness.CleanAsync(TestContext.Current.CancellationToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.CleanAsync(canceled.Token));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "direct-async-disposal-stops-running-bus")]
    public async Task DisposeAsync_StopsARunningBusCancelsItsScopeAndIsIdempotentAsync()
    {
        TimeSpan timeout = OperationTimeout();
        var harness = CreateHarness(timeout);
        await harness.StartAsync(TestContext.Current.CancellationToken);
        CancellationToken scopeToken = harness.TestCancellationToken;

        await harness.DisposeAsync();
        await harness.DisposeAsync();

        Assert.True(scopeToken.IsCancellationRequested);
        Assert.Throws<InvalidOperationException>(() => harness.BusControl);
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            harness.StartAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            harness.StopAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "direct-sync-disposal-stops-running-bus")]
    public async Task Dispose_StopsARunningBusAndClearsItsOwnedEndpointsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        var harness = CreateHarness(timeout);
        await harness.StartAsync(TestContext.Current.CancellationToken);

        harness.Dispose();

        Assert.Throws<InvalidOperationException>(() => harness.BusControl);
        Assert.Throws<InvalidOperationException>(() => harness.BusSendEndpoint);
        Assert.Throws<InvalidOperationException>(() => harness.InputQueueSendEndpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "subscription-caller-cancellation")]
    public async Task WaitForMessageAsync_RemainsCancelableAfterRegistrationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        using var harness = CreateHarness(timeout);
        await harness.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            using var cancellationSource = new CancellationTokenSource();
            Task<ConsumeContext<RequestMessage>> pending = harness.WaitForMessageAsync<RequestMessage>(cancellationSource.Token);

            cancellationSource.Cancel();

            OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"testing-harness-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static async Task SendAndAssertConsumedAsync(
        ConsumerTestHarness<NamedConsumer> consumer,
        string queueName,
        InMemoryTestHarness harness,
        CancellationToken cancellationToken)
    {
        ISendEndpoint endpoint = await harness.GetSendEndpointAsync(
            new Uri(harness.BaseAddress, queueName),
            cancellationToken);
        await endpoint.SendAsync(new NamedConsumerMessage(queueName), cancellationToken);

        IConsumedMessage<NamedConsumerMessage> received = await consumer.Consumed
            .SelectAsync<NamedConsumerMessage>(cancellationToken)
            .FirstObservedAsync(cancellationToken: cancellationToken);

        Assert.Equal(queueName, received.Context.Message.Value);
        Assert.Equal(new Uri(harness.BaseAddress, queueName), received.Context.Advanced().ReceiveContext.InputAddress);
        Assert.Null(received.Exception);
    }

    private sealed record RequestMessage(string Value);

    private sealed record ReplyMessage(string Value);

    private sealed class ReplyingConsumer : IConsumer<RequestMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RequestMessage> context) =>
            context.RespondAsync(new ReplyMessage($"reply:{context.Message.Value}"));
    }

    private sealed record GatedMessage;

    private sealed class GatedConsumer(
        TaskCompletionSource<bool> entered,
        TaskCompletionSource<bool> release) : IConsumer<GatedMessage>
    {
        public async Task ConsumeAsync(ConsumeContext<GatedMessage> context)
        {
            entered.TrySetResult(true);
            await release.Task.WaitAsync(context.CancellationToken);
            await context.RespondAsync(new GatedResponse("completed"));
        }
    }

    private sealed record GatedResponse(string Value);

    private sealed record FirstRequest;

    private sealed record FirstResponse;

    private sealed record SecondRequest;

    private sealed record SecondResponse;

    private sealed class MultiContractConsumer :
        IConsumer<FirstRequest>,
        IConsumer<SecondRequest>
    {
        public Task ConsumeAsync(ConsumeContext<FirstRequest> context) => context.RespondAsync(new FirstResponse());

        public Task ConsumeAsync(ConsumeContext<SecondRequest> context) => context.RespondAsync(new SecondResponse());
    }

    public interface IRequestContract
    {
        string Value { get; }
    }

    private sealed record ConcreteRequest(string Value) : IRequestContract;

    public interface IReplyContract
    {
        string Value { get; }
    }

    private sealed record ConcreteReply(string Value) : IReplyContract;

    private sealed class InterfaceConsumer : IConsumer<IRequestContract>
    {
        public Task ConsumeAsync(ConsumeContext<IRequestContract> context) =>
            context.RespondAsync(new ConcreteReply(context.Message.Value));
    }

    private sealed record HandlerRequest(int Value);

    private sealed record HandlerResponse(int Value);

    private sealed record FailingHandlerMessage;

    private sealed record PlainWaitMessage(string Value);

    private sealed record FilteredWaitMessage(string Value, bool Accept);

    private sealed record CountedWaitMessage(int Value);

    private sealed record DelegateWaitMessage(int Value);

    private sealed record DelegateWaitResponse(int Value);

    private sealed record ConsumerWaitMessage(string Value);

    private sealed record SubscriptionWaitMessage(string Value, bool Accept);

    private sealed record PassiveHandlerMessage(string Value);

    private sealed record UnconsumedPassiveMessage(string Value);

    public interface IPassiveHandlerContract
    {
        string Value { get; }
    }

    private sealed record ConcretePassiveHandlerMessage(string Value) : IPassiveHandlerContract;

    private sealed record FailingConsumerMessage;

    private sealed class FailingConsumer(Exception exception) : IConsumer<FailingConsumerMessage>
    {
        public Task ConsumeAsync(ConsumeContext<FailingConsumerMessage> context) => Task.FromException(exception);
    }

    private sealed record NamedConsumerMessage(string Value);

    private sealed class NamedConsumer : IConsumer<NamedConsumerMessage>
    {
        public Task ConsumeAsync(ConsumeContext<NamedConsumerMessage> context) => Task.CompletedTask;
    }
}
