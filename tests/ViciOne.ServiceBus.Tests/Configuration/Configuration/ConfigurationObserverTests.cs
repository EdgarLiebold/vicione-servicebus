using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class ConfigurationObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONFIGURATION-OBSERVATION", "generic-default-and-runtime-factory-registration")]
    public async Task ConsumerRegistrationShapes_ReportTheExactConsumerAndMessageContractsAsync()
    {
        Action<IInMemoryReceiveEndpointConfigurator>[] registrations =
        [
            endpoint => endpoint.Consumer<DualConsumer>(consumer =>
            {
                consumer.Message<AlphaMessage>(message => message.UseExecute(_ => { }));
                consumer.Message<ZuluMessage>(message => message.UseExecute(_ => { }));
                consumer.ConsumerMessage<AlphaMessage>(message => message.UseExecute(_ => { }));
                consumer.UseExecuteAsync(_ => Task.CompletedTask);
            }),
            endpoint => endpoint.Consumer<DualConsumer>(),
            endpoint => endpoint.Consumer(typeof(DualConsumer), requestedType =>
            {
                Assert.Equal(typeof(DualConsumer), requestedType);
                return new DualConsumer();
            }),
        ];

        foreach (Action<IInMemoryReceiveEndpointConfigurator> register in registrations)
        {
            var observer = new RecordingConsumerObserver();
            IBusControl bus = Bus.Factory.CreateUsingInMemory(configurator =>
            {
                configurator.ConnectConsumerConfigurationObserver(observer);
                configurator.ReceiveEndpoint($"consumer-observer-{NewId.NextGuid():N}", register);
            });

            Assert.Equal([typeof(DualConsumer)], observer.ConsumerTypes);
            Assert.Equal(
                [(typeof(DualConsumer), typeof(AlphaMessage)), (typeof(DualConsumer), typeof(ZuluMessage))],
                observer.MessageTypes.OrderBy(value => value.MessageType.Name, StringComparer.Ordinal));

            await bus.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HANDLER-AND-SAGA-CONFIGURATION-OBSERVATION", "exact-configured-contracts")]
    public async Task HandlerAndSagaRegistration_ReportEveryConfiguredContractExactlyOnceAsync()
    {
        var handlerObserver = new RecordingHandlerObserver();
        var sagaObserver = new RecordingSagaObserver();
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configurator =>
        {
            configurator.ConnectHandlerConfigurationObserver(handlerObserver);
            configurator.ConnectSagaConfigurationObserver(sagaObserver);
            configurator.ReceiveEndpoint($"handler-saga-observer-{NewId.NextGuid():N}", endpoint =>
            {
                endpoint.Handler<HandlerMessage>(_ => Task.CompletedTask);
                endpoint.Saga(new InMemorySagaRepository<ObservedSaga>(), saga =>
                {
                    saga.Message<SagaStarted>(message => message.UseExecute(_ => { }));
                    saga.Message<SagaContinued>(message => message.UseExecute(_ => { }));
                    saga.SagaMessage<SagaStarted>(message => message.UseExecute(_ => { }));
                    saga.UseExecuteAsync(_ => Task.CompletedTask);
                });
            });
        });

        Assert.Equal([typeof(HandlerMessage)], handlerObserver.MessageTypes);
        Assert.Equal([typeof(ObservedSaga)], sagaObserver.SagaTypes);
        Assert.Equal(
            [(typeof(ObservedSaga), typeof(SagaContinued)), (typeof(ObservedSaga), typeof(SagaStarted))],
            sagaObserver.MessageTypes.OrderBy(value => value.MessageType.Name, StringComparer.Ordinal));

        await bus.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVITY-CONFIGURATION-OBSERVATION", "execute-compensatable-and-compensate-hosts")]
    public async Task ActivityRegistration_ReportsExactTypesAndCompensationAddressAsync()
    {
        var observer = new RecordingActivityObserver();
        Uri? compensationAddress = null;
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configurator =>
        {
            configurator.ConnectActivityConfigurationObserver(observer);
            configurator.ReceiveEndpoint($"compensate-observer-{NewId.NextGuid():N}", compensateEndpoint =>
            {
                compensateEndpoint.CompensateActivityHost<CompensatingActivity, ActivityLog>();
                compensationAddress = compensateEndpoint.InputAddress;
            });
            configurator.ReceiveEndpoint($"activity-observer-{NewId.NextGuid():N}", endpoint =>
            {
                endpoint.ExecuteActivityHost<ExecuteOnlyActivity, ActivityArguments>();
                endpoint.ExecuteActivityHost<CompensatingActivity, ActivityArguments>(compensationAddress!);
            });
        });

        Uri expectedCompensationAddress = Assert.IsType<Uri>(compensationAddress);
        Assert.Equal([(typeof(ExecuteOnlyActivity), typeof(ActivityArguments))], observer.ExecuteOnly);
        Assert.Equal([(typeof(CompensatingActivity), typeof(ActivityArguments), expectedCompensationAddress)], observer.Compensatable);
        Assert.Equal([(typeof(CompensatingActivity), typeof(ActivityLog))], observer.Compensate);

        await bus.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-LIFECYCLE", "notify-before-validation-exactly-once")]
    public void ObserverInjectedSpecifications_AreValidatedOnTheFirstAndEverySubsequentSnapshot()
    {
        var consumerObserver = new InjectingConsumerObserver();
        var consumer = new ConsumerConfigurator<DualConsumer>(new DelegateConsumerFactory<DualConsumer>(() => new DualConsumer()), consumerObserver);
        AssertStableObserverValidation(consumer.Validate, consumerObserver.ExpectedKeys, () => consumerObserver.NotificationCount);

        var handlerObserver = new InjectingHandlerObserver();
        var handler = new HandlerConfigurator<HandlerMessage>(_ => Task.CompletedTask, handlerObserver);
        AssertStableObserverValidation(handler.Validate, handlerObserver.ExpectedKeys, () => handlerObserver.NotificationCount);

        var sagaObserver = new InjectingSagaObserver();
        var saga = new SagaConfigurator<ObservedSaga>(new InMemorySagaRepository<ObservedSaga>(), sagaObserver);
        AssertStableObserverValidation(saga.Validate, sagaObserver.ExpectedKeys, () => sagaObserver.NotificationCount);

        var activityObserver = new InjectingActivityObserver();
        var execute = new ExecuteActivityHostConfigurator<ExecuteOnlyActivity, ActivityArguments>(
            DefaultConstructorExecuteActivityFactory<ExecuteOnlyActivity, ActivityArguments>.ExecuteFactory,
            activityObserver);
        AssertStableObserverValidation(execute.Validate, ["execute-activity"], () => activityObserver.ExecuteNotificationCount);

        var compensate = new CompensateActivityHostConfigurator<CompensatingActivity, ActivityLog>(
            DefaultConstructorCompensateActivityFactory<CompensatingActivity, ActivityLog>.CompensateFactory,
            activityObserver);
        AssertStableObserverValidation(compensate.Validate, ["compensate-activity"], () => activityObserver.CompensateNotificationCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVER-FAILURE", "sticky-failure-and-reentrancy-rejection")]
    public void ObserverNotification_PreservesTheFirstFailureAndRejectsReentrantValidation()
    {
        var failingObserver = new FailingHandlerObserver();
        var faulted = new HandlerConfigurator<HandlerMessage>(_ => Task.CompletedTask, failingObserver);

        ExpectedObserverException first = Assert.Throws<ExpectedObserverException>(() => faulted.Validate().ToArray());
        ExpectedObserverException second = Assert.Throws<ExpectedObserverException>(() => faulted.Validate().ToArray());

        Assert.Same(first, second);
        Assert.Equal(1, failingObserver.NotificationCount);

        var reentrantObserver = new ReentrantHandlerObserver();
        var reentrant = new HandlerConfigurator<HandlerMessage>(_ => Task.CompletedTask, reentrantObserver);
        reentrantObserver.Validate = reentrant.Validate;

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => reentrant.Validate().ToArray());
        Assert.Equal(
            "Configuration Observer Notification for bus 'unknown': A configuration observer re-entered notification for the same configuration object. Correct the named configuration before starting the host.",
            exception.Message);
    }

    private static void AssertStableObserverValidation(
        Func<IEnumerable<ValidationResult>> validate,
        IReadOnlyCollection<string> expectedKeys,
        Func<int> notificationCount)
    {
        ValidationResult[] first = validate().ToArray();
        ValidationResult[] second = validate().ToArray();

        Assert.Equal(expectedKeys.Order(StringComparer.Ordinal), first.Select(result => result.Key).Order(StringComparer.Ordinal));
        Assert.Equal(expectedKeys.Order(StringComparer.Ordinal), second.Select(result => result.Key).Order(StringComparer.Ordinal));
        Assert.All(first.Concat(second), result => Assert.Equal(ValidationResultDisposition.Failure, result.Disposition));
        Assert.Equal(1, notificationCount());
    }

    public sealed record AlphaMessage;

    public sealed record ZuluMessage;

    public sealed class DualConsumer : IConsumer<AlphaMessage>, IConsumer<ZuluMessage>
    {
        public Task ConsumeAsync(ConsumeContext<AlphaMessage> context) => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<ZuluMessage> context) => Task.CompletedTask;
    }

    public sealed record HandlerMessage;

    public sealed record SagaStarted(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record SagaContinued(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class ObservedSaga : ISaga, InitiatedBy<SagaStarted>, Orchestrates<SagaContinued>
    {
        public ObservedSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<SagaStarted> context) => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<SagaContinued> context) => Task.CompletedTask;
    }

    public sealed record ActivityArguments;

    public sealed record ActivityLog;

    public sealed class ExecuteOnlyActivity : IExecuteActivity<ActivityArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ActivityArguments> context) => Task.FromResult(context.Completed());
    }

    public sealed class CompensatingActivity : IActivity<ActivityArguments, ActivityLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ActivityArguments> context) =>
            Task.FromResult(context.Completed(new ActivityLog()));

        public Task<CompensationResult> CompensateAsync(CompensateContext<ActivityLog> context) =>
            Task.FromResult(context.Compensated());
    }

    private sealed class RecordingConsumerObserver : IConsumerConfigurationObserver
    {
        public List<Type> ConsumerTypes { get; } = [];

        public List<(Type ConsumerType, Type MessageType)> MessageTypes { get; } = [];

        public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
            where TConsumer : class => ConsumerTypes.Add(typeof(TConsumer));

        public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
            where TConsumer : class
            where TMessage : class => MessageTypes.Add((typeof(TConsumer), typeof(TMessage)));
    }

    private sealed class RecordingHandlerObserver : IHandlerConfigurationObserver
    {
        public List<Type> MessageTypes { get; } = [];

        public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
            where TMessage : class => MessageTypes.Add(typeof(TMessage));
    }

    private sealed class RecordingSagaObserver : ISagaConfigurationObserver
    {
        public List<Type> SagaTypes { get; } = [];

        public List<(Type SagaType, Type MessageType)> MessageTypes { get; } = [];

        public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
            where TSaga : class => SagaTypes.Add(typeof(TSaga));

        public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
            where TInstance : class
        {
        }

        public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
            where TSaga : class
            where TMessage : class => MessageTypes.Add((typeof(TSaga), typeof(TMessage)));
    }

    private sealed class RecordingActivityObserver : IActivityConfigurationObserver
    {
        public List<(Type ActivityType, Type ArgumentsType, Uri CompensateAddress)> Compensatable { get; } = [];

        public List<(Type ActivityType, Type ArgumentsType)> ExecuteOnly { get; } = [];

        public List<(Type ActivityType, Type LogType)> Compensate { get; } = [];

        public void ActivityConfigured<TActivity, TArguments>(
            IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
            Uri compensateAddress)
            where TActivity : class
            where TArguments : class => Compensatable.Add((typeof(TActivity), typeof(TArguments), compensateAddress));

        public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
            where TActivity : class
            where TArguments : class => ExecuteOnly.Add((typeof(TActivity), typeof(TArguments)));

        public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
            where TActivity : class
            where TLog : class => Compensate.Add((typeof(TActivity), typeof(TLog)));
    }

    private sealed class InjectingConsumerObserver : IConsumerConfigurationObserver
    {
        public string[] ExpectedKeys => ["consumer-root", "consumer-root", "message-alpha", "message-zulu"];

        public int NotificationCount { get; private set; }

        public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
            where TConsumer : class
        {
            NotificationCount++;
            configurator.AddPipeSpecification(new InvalidSpecification<ConsumerConsumeContext<TConsumer>>(
                typeof(TConsumer) == typeof(DualConsumer) ? "consumer-root" : "unexpected"));
        }

        public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
            where TConsumer : class
            where TMessage : class
        {
            string suffix = typeof(TMessage) == typeof(AlphaMessage) ? "alpha" : "zulu";
            configurator.AddPipeSpecification(new InvalidSpecification<ConsumerConsumeContext<TConsumer, TMessage>>($"message-{suffix}"));
        }
    }

    private sealed class InjectingHandlerObserver : IHandlerConfigurationObserver
    {
        public string[] ExpectedKeys => ["handler"];

        public int NotificationCount { get; private set; }

        public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
            where TMessage : class
        {
            NotificationCount++;
            configurator.AddPipeSpecification(new InvalidSpecification<ConsumeContext<TMessage>>("handler"));
        }
    }

    private sealed class FailingHandlerObserver : IHandlerConfigurationObserver
    {
        public int NotificationCount { get; private set; }

        public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
            where TMessage : class
        {
            NotificationCount++;
            configurator.AddPipeSpecification(new InvalidSpecification<ConsumeContext<TMessage>>("partial-observer-side-effect"));
            throw new ExpectedObserverException();
        }
    }

    private sealed class ReentrantHandlerObserver : IHandlerConfigurationObserver
    {
        public Func<IEnumerable<ValidationResult>>? Validate { get; set; }

        public void HandlerConfigured<TMessage>(IHandlerConfigurator<TMessage> configurator)
            where TMessage : class
        {
            Assert.NotNull(Validate);
            Validate().ToArray();
        }
    }

    private sealed class InjectingSagaObserver : ISagaConfigurationObserver
    {
        public string[] ExpectedKeys => ["saga-root", "saga-root", "saga-message-alpha", "saga-message-zulu"];

        public int NotificationCount { get; private set; }

        public void SagaConfigured<TSaga>(ISagaConfigurator<TSaga> configurator)
            where TSaga : class
        {
            NotificationCount++;
            configurator.AddPipeSpecification(new InvalidSpecification<SagaConsumeContext<TSaga>>("saga-root"));
        }

        public void StateMachineSagaConfigured<TInstance>(ISagaConfigurator<TInstance> configurator, object stateMachine)
            where TInstance : class
        {
        }

        public void SagaMessageConfigured<TSaga, TMessage>(ISagaMessageConfigurator<TSaga, TMessage> configurator)
            where TSaga : class
            where TMessage : class
        {
            string suffix = typeof(TMessage) == typeof(SagaStarted) ? "alpha" : "zulu";
            configurator.AddPipeSpecification(new InvalidSpecification<SagaConsumeContext<TSaga, TMessage>>($"saga-message-{suffix}"));
        }
    }

    private sealed class InjectingActivityObserver : IActivityConfigurationObserver
    {
        public int ExecuteNotificationCount { get; private set; }

        public int CompensateNotificationCount { get; private set; }

        public void ActivityConfigured<TActivity, TArguments>(
            IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
            Uri compensateAddress)
            where TActivity : class
            where TArguments : class => throw new InvalidOperationException("This test configures execute-only and compensate-only hosts.");

        public void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
            where TActivity : class
            where TArguments : class
        {
            ExecuteNotificationCount++;
            configurator.AddPipeSpecification(new InvalidSpecification<ExecuteActivityContext<TActivity, TArguments>>("execute-activity"));
        }

        public void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
            where TActivity : class
            where TLog : class
        {
            CompensateNotificationCount++;
            configurator.AddPipeSpecification(new InvalidSpecification<CompensateActivityContext<TActivity, TLog>>("compensate-activity"));
        }
    }

    private sealed class InvalidSpecification<TContext>(string key) : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public void Apply(IPipeBuilder<TContext> builder) => ArgumentNullException.ThrowIfNull(builder);

        public IEnumerable<ValidationResult> Validate()
        {
            yield return this.Failure(key, $"{key} is intentionally invalid");
        }
    }

    private sealed class ExpectedObserverException : Exception
    {
    }
}
