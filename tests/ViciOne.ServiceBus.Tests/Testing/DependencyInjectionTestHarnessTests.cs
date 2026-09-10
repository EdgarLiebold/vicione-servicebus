using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Testing.Internal;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class DependencyInjectionTestHarnessTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "existing-default-bus-fails-without-mutation")]
    public void ExistingDefaultBus_IsRejectedWithoutMutatingItsRegistrations()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
            configuration.UsingInMemory(static (_, _) => { }));
        ServiceDescriptor[] before = services.ToArray();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            services.AddViciOneServiceBusTestHarness());

        Assert.Equal(
            "Test harness for bus 'default': IBus is already registered. Configure AddViciOneServiceBusTestHarness as the only default-bus registration.",
            exception.Message);
        Assert.Equal(before, services);
        Assert.Single(services, static descriptor => descriptor.ServiceType == typeof(IBus));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "scoped-service-publishes-through-started-bus")]
    public async Task ScopedApplicationService_PublishesThroughTheStartedHarnessBusAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddScoped<ApplicationPublisher>()
            .AddViciOneServiceBusTestHarness(configuration => configuration.SetTestTimeouts(timeout, timeout))
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            ApplicationPublisher service = harness.Scope.ServiceProvider.GetRequiredService<ApplicationPublisher>();
            await service.PublishAsync(cancellationToken);

            IPublishedMessage<ApplicationEvent> published = await harness.Published
                .SelectAsync<ApplicationEvent>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("registered", published.Context.Message.Value);
            Assert.Null(published.Exception);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "custom-time-provider-propagation")]
    public async Task RegisteredTimeProvider_IsTheSingleHarnessTimeSourceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FakeTimeProvider(StartTime);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(timeProvider)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<TimeAwareConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Assert.Same(timeProvider, harness.TimeProvider);
            Assert.Same(timeProvider, provider.GetRequiredService<TimeProvider>());

            await harness.Bus.PublishAsync(new TimeAwareMessage(), cancellationToken);
            IConsumerTestHarness<TimeAwareConsumer> consumer = harness.GetConsumerHarness<TimeAwareConsumer>();

            Assert.True(await consumer.Consumed.AnyAsync<TimeAwareMessage>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "default-system-time-provider")]
    public async Task DefaultRegistration_UsesTheStandardSystemTimeProviderAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness()
            .BuildServiceProvider(validateScopes: true);

        ITestHarness harness = provider.GetTestHarness();

        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.Same(TimeProvider.System, harness.TimeProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "missing-hosted-service-rejected")]
    public async Task ContainerHarnessWithoutHostedServices_FailsAtStartWithAnExactConfigurationErrorAsync()
    {
        await using ServiceProvider provider = new ServiceCollection().BuildServiceProvider(validateScopes: true);
        var options = Options.Create(new TestHarnessOptions());
        await using var harness = new ContainerTestHarness(provider, options, TimeProvider.System);

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() => harness.StartAsync(TestContext.Current.CancellationToken));

        Assert.Equal(
            "Test harness for bus 'unknown': The ViciOne.ServiceBus hosted service was not found. Correct the named configuration before starting the host.",
            exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "scoped-request-client-response")]
    public async Task ScopedRequestClient_RecordsTheExactRequestAndResponseAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<RequestConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = NewId.NextGuid();
            IRequestClient<RequestMessage> client = harness.GetRequestClient<RequestMessage>();

            Response<ResponseMessage> response = await client.GetResponseAsync<ResponseMessage>(
                new RequestMessage(correlationId),
                cancellationToken);
            IConsumedMessage<RequestMessage> consumed = await harness.Consumed
                .SelectAsync<RequestMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            ISentMessage<ResponseMessage> sent = await harness.Sent
                .SelectAsync<ResponseMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal(correlationId, consumed.Context.Message.CorrelationId);
            Assert.Equal(correlationId, sent.Context.Message.CorrelationId);
            Assert.Equal(consumed.Context.RequestId, sent.Context.RequestId);
            Assert.Null(consumed.Exception);
            Assert.Null(sent.Exception);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "retry-produces-one-terminal-fault")]
    public async Task RetryPolicy_ExecutesTheConfiguredAttemptsButRecordsOneTerminalFaultAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var counter = new AttemptCounter();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(counter)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<RetryingConsumer>();
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                    endpoint.UseMessageRetry(retry => retry.Immediate(3)));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = NewId.NextGuid();
            Task<IPublishedMessage<Fault<RetryingMessage>>> terminalFaultTask = harness.Published
                .SelectAsync<Fault<RetryingMessage>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IConsumedMessage<RetryingMessage>> terminalConsumeTask = harness.Consumed
                .SelectAsync<RetryingMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.Bus.PublishAsync(new RetryingMessage(correlationId), cancellationToken);
            IPublishedMessage<Fault<RetryingMessage>> terminalFault = await terminalFaultTask.WaitAsync(timeout, cancellationToken);
            IConsumedMessage<RetryingMessage> terminalConsume = await terminalConsumeTask.WaitAsync(timeout, cancellationToken);
            IPublishedMessage<Fault<RetryingMessage>>[] faults = harness.Published
                .Snapshot()
                .OfType<IPublishedMessage<Fault<RetryingMessage>>>()
                .ToArray();
            IConsumedMessage<RetryingMessage>[] consumed = harness.Consumed
                .Snapshot()
                .OfType<IConsumedMessage<RetryingMessage>>()
                .ToArray();

            Assert.Equal(4, counter.Count);
            Assert.Single(faults);
            Assert.Single(consumed);
            Assert.Same(terminalConsume, consumed[0]);
            Assert.Equal(correlationId, terminalFault.Context.Message.Message.CorrelationId);
            Assert.Single(terminalFault.Context.Message.Exceptions);
            Assert.EndsWith(nameof(ExpectedRetryException), terminalFault.Context.Message.Exceptions[0].ExceptionType);
            Assert.Equal("retry failed", terminalFault.Context.Message.Exceptions[0].Message);
            Assert.IsType<ExpectedRetryException>(consumed[0].Exception);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "in-memory-saga-repository-management")]
    public async Task InMemorySagaManagement_AddsUpdatesRemovesAndRejectsDuplicatesExactlyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSaga<ManagedSaga>().InMemoryRepository();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = provider.GetTestHarness();
        IndexedSagaDictionary<ManagedSaga> repository =
            provider.GetRequiredService<IndexedSagaDictionary<ManagedSaga>>();
        Guid correlationId = NewId.NextGuid();

        harness.AddSagaInstance<ManagedSaga>(correlationId, saga => saga.Value = "initial");

        Assert.Equal(1, repository.Count);
        Assert.Equal("initial", Assert.IsType<ViciOne.ServiceBus.Saga.SagaInstance<ManagedSaga>>(repository[correlationId]).Instance.Value);
        ArgumentException duplicate = Assert.Throws<ArgumentException>(() =>
            harness.AddSagaInstance<ManagedSaga>(correlationId));
        Assert.Equal("correlationId", duplicate.ParamName);

        await harness.AddOrUpdateSagaInstanceAsync<ManagedSaga>(
            correlationId,
            saga => saga.Value = "updated",
            TestContext.Current.CancellationToken);

        Assert.Equal(1, repository.Count);
        Assert.Equal("updated", Assert.IsType<ViciOne.ServiceBus.Saga.SagaInstance<ManagedSaga>>(repository[correlationId]).Instance.Value);
        Assert.True(await harness.TryRemoveSagaInstanceAsync<ManagedSaga>(
            correlationId,
            TestContext.Current.CancellationToken));
        Assert.Equal(0, repository.Count);
        Assert.False(await harness.TryRemoveSagaInstanceAsync<ManagedSaga>(
            correlationId,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "consumer-and-bus-observation-boundaries")]
    public async Task ConsumerHarness_RecordsTheSameRequestAtItsConsumerAndBusBoundariesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<RequestConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = NewId.NextGuid();
            IRequestClient<RequestMessage> client = harness.GetRequestClient<RequestMessage>();
            IConsumerTestHarness<RequestConsumer> consumerHarness = harness.GetConsumerHarness<RequestConsumer>();

            Response<ResponseMessage> response = await client.GetResponseAsync<ResponseMessage>(
                new RequestMessage(correlationId),
                cancellationToken);
            IConsumedMessage<RequestMessage> busObservation = await harness.Consumed
                .SelectAsync<RequestMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IConsumedMessage<RequestMessage> consumerObservation = await consumerHarness.Consumed
                .SelectAsync<RequestMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal(correlationId, busObservation.Context.Message.CorrelationId);
            Assert.Equal(correlationId, consumerObservation.Context.Message.CorrelationId);
            Assert.Equal(busObservation.Context.MessageId, consumerObservation.Context.MessageId);
            Assert.Null(busObservation.Exception);
            Assert.Null(consumerObservation.Exception);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "scoped-consumer-dependency-publishes-follow-up")]
    public async Task ScopedConsumerDependency_PublishesTheExactCausallyLinkedFollowUpMessageAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddScoped<ScopedConsumerDependency>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DependencyPublishingConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = NewId.NextGuid();

            await harness.Bus.PublishAsync(new SubmitThroughDependency(correlationId, "expected"), cancellationToken);

            IConsumedMessage<SubmitThroughDependency> consumed = await harness.Consumed
                .SelectAsync<SubmitThroughDependency>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IPublishedMessage<SubmittedThroughDependency> published = await harness.Published
                .SelectAsync<SubmittedThroughDependency>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IConsumerTestHarness<DependencyPublishingConsumer> consumerHarness =
                harness.GetConsumerHarness<DependencyPublishingConsumer>();

            Assert.True(await consumerHarness.Consumed.AnyAsync<SubmitThroughDependency>(cancellationToken));
            Assert.Null(consumed.Exception);
            Assert.Equal(correlationId, consumed.Context.CorrelationId);
            Assert.Equal(correlationId, published.Context.Message.CorrelationId);
            Assert.Equal("expected", published.Context.Message.Value);
            Assert.Equal(consumed.Context.CorrelationId, published.Context.InitiatorId);
            Assert.Equal(consumed.Context.ConversationId, published.Context.ConversationId);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "classic-saga-registration-and-observations")]
    public async Task ContainerSagaHarness_RecordsCreationStateConsumptionAndPublicationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSaga<SagaTestHarnessBehaviorTests.ClassicSaga>().InMemoryRepository();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = NewId.NextGuid();
            var message = new SagaTestHarnessBehaviorTests.StartSaga(
                sagaId,
                "container",
                [new SagaTestHarnessBehaviorTests.SagaValue("first"), new SagaTestHarnessBehaviorTests.SagaValue("second")]);

            await harness.Bus.PublishAsync(message, cancellationToken);

            ISagaTestHarness<SagaTestHarnessBehaviorTests.ClassicSaga> sagaHarness =
                harness.GetSagaHarness<SagaTestHarnessBehaviorTests.ClassicSaga>();
            IConsumedMessage<SagaTestHarnessBehaviorTests.StartSaga> received = await sagaHarness.Consumed
                .SelectAsync<SagaTestHarnessBehaviorTests.StartSaga>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            SagaTestHarnessBehaviorTests.ClassicSaga? created = sagaHarness.Created.FindById(sagaId);
            SagaTestHarnessBehaviorTests.ClassicSaga? observed = sagaHarness.Sagas.FindById(sagaId);
            IPublishedMessage<SagaTestHarnessBehaviorTests.SagaStarted> published = await harness.Published
                .SelectAsync<SagaTestHarnessBehaviorTests.SagaStarted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(sagaId, received.Context.Message.CorrelationId);
            Assert.Null(received.Exception);
            Assert.NotNull(created);
            Assert.Same(created, observed);
            Assert.Equal("container", created.Value);
            Assert.Equal(["first", "second"], created.Values);
            Assert.Equal(sagaId, published.Context.Message.CorrelationId);
            Assert.Equal("container", published.Context.Message.Value);
            Assert.Empty(harness.Published.Snapshot().OfType<IPublishedMessage<UnexpectedSagaPublication>>());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "state-machine-endpoint-causation-and-state")]
    public async Task ContainerStateMachineHarness_PreservesCausationAndRecordsTheResultingStateAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddScoped<PublishContainerStartedActivity>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSagaStateMachine<ContainerMetadataStateMachine, ContainerMetadataState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = NewId.NextGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<ContainerMetadataState>(TestContext.Current.CancellationToken);

            await endpoint.SendAsync(new ContainerStart(sagaId, "expected"), cancellationToken);

            IConsumedMessage<ContainerStart> received = await harness.Consumed
                .SelectAsync<ContainerStart>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IPublishedMessage<ContainerStarted> published = await harness.Published
                .SelectAsync<ContainerStarted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            ISagaStateMachineTestHarness<ContainerMetadataStateMachine, ContainerMetadataState> sagaHarness =
                harness.GetSagaStateMachineHarness<ContainerMetadataStateMachine, ContainerMetadataState>();
            Guid? running = await sagaHarness.WaitForSagaInStateAsync(sagaId, machine => machine.Running, timeout, TestContext.Current.CancellationToken);
            ContainerMetadataState? created = sagaHarness.Created
                .Snapshot()
                .Where(instance => instance.Saga.CorrelationId == sagaId)
                .Select(instance => instance.Saga)
                .SingleOrDefault();

            Assert.Equal(sagaId, received.Context.CorrelationId);
            Assert.Equal(sagaId, received.Context.Message.CorrelationId);
            Assert.Null(received.Exception);
            Assert.Equal(sagaId, published.Context.Message.CorrelationId);
            Assert.Equal("expected", published.Context.Message.Value);
            Assert.Equal(sagaId, published.Context.InitiatorId);
            Assert.Equal(received.Context.ConversationId, published.Context.ConversationId);
            Assert.Equal(sagaId, running);
            Assert.NotNull(created);
            Assert.Equal("expected", created.Value);
            Assert.Equal(sagaHarness.StateMachine.Running.Name, created.CurrentState);
            Assert.True(await sagaHarness.Consumed.AnyAsync<ContainerStart>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DI", "state-machine-request-response-and-state")]
    public async Task ContainerStateMachineRequestClient_RespondsAndRecordsTheExactStateAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSagaStateMachine<
                        SagaTestHarnessBehaviorTests.ResponsiveStateMachine,
                        SagaTestHarnessBehaviorTests.ResponsiveState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = NewId.NextGuid();
            IRequestClient<SagaTestHarnessBehaviorTests.ResponsiveRequest> client =
                harness.GetRequestClient<SagaTestHarnessBehaviorTests.ResponsiveRequest>();

            Response<SagaTestHarnessBehaviorTests.ResponsiveResponse> response =
                await client.GetResponseAsync<SagaTestHarnessBehaviorTests.ResponsiveResponse>(
                    new SagaTestHarnessBehaviorTests.ResponsiveRequest(sagaId, "container"),
                    cancellationToken);
            ISagaStateMachineTestHarness<
                    SagaTestHarnessBehaviorTests.ResponsiveStateMachine,
                    SagaTestHarnessBehaviorTests.ResponsiveState> sagaHarness =
                harness.GetSagaStateMachineHarness<
                    SagaTestHarnessBehaviorTests.ResponsiveStateMachine,
                    SagaTestHarnessBehaviorTests.ResponsiveState>();
            Guid? responded = await sagaHarness.WaitForSagaInStateAsync(sagaId, machine => machine.Responded, timeout, TestContext.Current.CancellationToken);
            SagaTestHarnessBehaviorTests.ResponsiveState? state = sagaHarness.Sagas.FindById(sagaId);

            Assert.Equal(sagaId, response.Message.CorrelationId);
            Assert.Equal("response:container", response.Message.Value);
            Assert.Equal(sagaId, responded);
            Assert.NotNull(state);
            Assert.Equal("container", state.Value);
            Assert.Equal(sagaHarness.StateMachine.Responded.Name, state.CurrentState);
            Assert.True(await sagaHarness.Consumed.AnyAsync<SagaTestHarnessBehaviorTests.ResponsiveRequest>(cancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class ApplicationPublisher(IBus bus)
    {
        public Task PublishAsync(CancellationToken cancellationToken) =>
            bus.PublishAsync(new ApplicationEvent("registered"), cancellationToken);
    }

    private sealed record ApplicationEvent(string Value);

    private sealed record TimeAwareMessage;

    private sealed class TimeAwareConsumer : IConsumer<TimeAwareMessage>
    {
        public Task ConsumeAsync(ConsumeContext<TimeAwareMessage> context) => Task.CompletedTask;
    }

    private sealed record RequestMessage(Guid CorrelationId);

    private sealed record ResponseMessage(Guid CorrelationId);

    private sealed record UnexpectedSagaPublication(Guid CorrelationId);

    private sealed record SubmitThroughDependency(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record SubmittedThroughDependency(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed class ScopedConsumerDependency(IPublishEndpoint publishEndpoint)
    {
        public Task PublishAsync(ConsumeContext<SubmitThroughDependency> context) =>
            publishEndpoint.PublishAsync(
                new SubmittedThroughDependency(context.Message.CorrelationId, context.Message.Value),
                context.CancellationToken);
    }

    private sealed class DependencyPublishingConsumer(ScopedConsumerDependency dependency) :
        IConsumer<SubmitThroughDependency>
    {
        public Task ConsumeAsync(ConsumeContext<SubmitThroughDependency> context) => dependency.PublishAsync(context);
    }

    private sealed class RequestConsumer : IConsumer<RequestMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RequestMessage> context) =>
            context.RespondAsync(new ResponseMessage(context.Message.CorrelationId));
    }

    private sealed record RetryingMessage(Guid CorrelationId);

    private sealed class RetryingConsumer(AttemptCounter counter) : IConsumer<RetryingMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RetryingMessage> context)
        {
            counter.Increment();
            return Task.FromException(new ExpectedRetryException("retry failed"));
        }
    }

    private sealed class AttemptCounter
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Increment() => Interlocked.Increment(ref _count);
    }

    private sealed class ExpectedRetryException(string message) : Exception(message);

    public sealed class ManagedSaga : ISaga
    {
        public Guid CorrelationId { get; set; }

        public string Value { get; set; } = string.Empty;
    }

    public sealed record ContainerStart(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record ContainerStarted(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed class ContainerMetadataState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }

    public sealed class ContainerMetadataStateMachine : ViciOneServiceBusStateMachine<ContainerMetadataState>
    {
        public ContainerMetadataStateMachine()
        {
            InstanceState(instance => instance.CurrentState);

            Event(() => Start, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.SelectId(context => context.Message.CorrelationId);
                configuration.InsertOnInitial = true;
            });

            Initially(
                When(Start)
                    .Then(context => context.Saga.Value = context.Message.Value)
                    .Activity(activity => activity.OfType<PublishContainerStartedActivity>())
                    .TransitionTo(Running));
        }

        public State Running { get; } = null!;

        public Event<ContainerStart> Start { get; } = null!;
    }

    public sealed class PublishContainerStartedActivity(IPublishEndpoint publishEndpoint) :
        IStateMachineActivity<ContainerMetadataState, ContainerStart>
    {
        public void Probe(ProbeContext context)
        {
        }

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task ExecuteAsync(
            BehaviorContext<ContainerMetadataState, ContainerStart> context,
            IBehavior<ContainerMetadataState, ContainerStart> next)
        {
            await publishEndpoint.PublishAsync(
                new ContainerStarted(context.Saga.CorrelationId, context.Saga.Value),
                context.CancellationToken);
            await next.ExecuteAsync(context);
        }

        public Task FaultedAsync<TException>(
            BehaviorExceptionContext<ContainerMetadataState, ContainerStart, TException> context,
            IBehavior<ContainerMetadataState, ContainerStart> next)
            where TException : Exception =>
            next.FaultedAsync(context);
    }
}
