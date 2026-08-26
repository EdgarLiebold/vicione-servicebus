using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class MessageRetryConfigurationExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY", "endpoint-policy-owns-failure")]
    public async Task EndpointRetry_OwnsTheFailureWithoutMultiplyingTheOuterBusBudget()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new RetryObservation(alwaysFail: true);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ObservedRetryConsumer>();
                configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                    endpoint.UseMessageRetry(retry => retry.Immediate(3)));
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UseMessageRetry(retry => retry.Immediate(1));
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Task<IPublishedMessage<Fault<ObservedRetryMessage>>> faultTask = harness.Published
                .SelectAsync<Fault<ObservedRetryMessage>>(cancellationToken)
                .First();

            await harness.Bus.Publish(new ObservedRetryMessage(), cancellationToken);
            await faultTask.WaitAsync(timeout, cancellationToken);

            Assert.Equal(4, observation.Count);
            Assert.Equal([0, 1, 2, 3], observation.Attempts);
            Assert.Equal([0, 0, 1, 2], observation.RetryCounts);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY", "consume-context-succeeds-with-exact-attempt-sequence")]
    public async Task ConsumerRetry_SucceedsOnTheThirdAttemptAndExposesEveryAttemptNumber()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new SuccessfulRetryObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<RetryUntilThirdAttemptConsumer>((_, consumer) =>
                    consumer.UseMessageRetry(retry => retry.Immediate(2)));
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            await harness.Bus.Publish(new RetryUntilThirdAttemptMessage(), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal([0, 1, 2], observation.Attempts);
            Assert.Equal([0, 0, 1], observation.RetryCounts);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY", "consumer-scoped-policy-budget")]
    public async Task ConsumerScopedRetry_AppliesOnlyTheConfiguredConsumerBudget(int retryLimit)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new RetryObservation(alwaysFail: true);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ObservedRetryConsumer>((_, consumer) =>
                    consumer.UseMessageRetry(retry => retry.Immediate(retryLimit)));
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Task<IPublishedMessage<Fault<ObservedRetryMessage>>> faultTask = harness.Published
                .SelectAsync<Fault<ObservedRetryMessage>>(cancellationToken)
                .First();

            await harness.Bus.Publish(new ObservedRetryMessage(), cancellationToken);
            await faultTask.WaitAsync(timeout, cancellationToken);

            Assert.Equal(retryLimit + 1, observation.Count);
            Assert.Equal(Enumerable.Range(0, retryLimit + 1), observation.Attempts);
            Assert.Equal([0, .. Enumerable.Range(0, retryLimit)], observation.RetryCounts);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY", "derived-message-base-and-interface-dispatch")]
    public async Task BusRetry_PreservesDerivedMessageDispatchThroughBaseAndInterfacePipelines()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new PolymorphicRetryObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<PolymorphicRetryConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UseMessageRetry(retry => retry.Immediate(1));
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = Guid.Parse("7d2d63fc-b3ba-462b-9dbb-97483394f61b");

            await harness.Bus.Publish(new DerivedRetryMessage(correlationId), cancellationToken);
            await Task.WhenAll(observation.BaseCompleted.Task, observation.InterfaceCompleted.Task)
                .WaitAsync(timeout, cancellationToken);

            Assert.Equal(2, observation.BaseCount);
            Assert.Equal(2, observation.InterfaceCount);
            Assert.Equal([0, 1], observation.BaseAttempts);
            Assert.Equal([0, 1], observation.InterfaceAttempts);
            Assert.Equal(correlationId, observation.BaseCorrelationId);
            Assert.Equal(correlationId, observation.InterfaceCorrelationId);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY", "disjoint-policies-keep-independent-ownership")]
    public async Task DisjointBusPolicies_AllowOnlyThePolicyHandlingTheFailureToConsumeItsBudget()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new RetryObservation(alwaysFail: true);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ObservedRetryConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UseMessageRetry(retry =>
                    {
                        retry.Ignore<TimeoutException>();
                        retry.Immediate(5);
                    });
                    bus.UseMessageRetry(retry =>
                    {
                        retry.Handle<TimeoutException>();
                        retry.Immediate(2);
                    });
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Task<IPublishedMessage<Fault<ObservedRetryMessage>>> faultTask = harness.Published
                .SelectAsync<Fault<ObservedRetryMessage>>(cancellationToken)
                .First();

            await harness.Bus.Publish(new ObservedRetryMessage(), cancellationToken);
            await faultTask.WaitAsync(timeout, cancellationToken);

            Assert.Equal(6, observation.Count);
            Assert.Equal([0, 1, 2, 3, 4, 5], observation.Attempts);
            Assert.Equal([0, 0, 1, 2, 3, 4], observation.RetryCounts);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY", "bus-stop-cancels-pending-retry")]
    public async Task StoppingTheBus_CancelsAPendingRetryWithoutStartingAnotherAttempt()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new RetryObservation(alwaysFail: true);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ObservedRetryConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.UseMessageRetry(retry => retry.Interval(1, TimeSpan.FromHours(1)));
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        var stopped = false;

        try
        {
            await harness.Bus.Publish(new ObservedRetryMessage(), cancellationToken);
            await observation.FirstAttempt.Task.WaitAsync(timeout, cancellationToken);

            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            stopped = true;

            Assert.Equal(1, observation.Count);
            Assert.Equal([0], observation.Attempts);
        }
        finally
        {
            if (!stopped)
                await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY", "none-and-default-run-once")]
    public async Task DefaultAndExplicitNone_ExecuteTheConsumerExactlyOnce(bool configureNone)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new RetryObservation(alwaysFail: true);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<ObservedRetryConsumer>();
                if (configureNone)
                {
                    configuration.AddConfigureEndpointsCallback((_, endpoint) =>
                        endpoint.UseMessageRetry(retry => retry.None()));
                }
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Task<IPublishedMessage<Fault<ObservedRetryMessage>>> faultTask = harness.Published
                .SelectAsync<Fault<ObservedRetryMessage>>(cancellationToken)
                .First();

            await harness.Bus.Publish(new ObservedRetryMessage(), cancellationToken);
            await faultTask.WaitAsync(timeout, cancellationToken);

            Assert.Equal(1, observation.Count);
            Assert.Equal([0], observation.Attempts);
            Assert.Equal([0], observation.RetryCounts);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY", "configuration-boundary-rejects-null-inputs")]
    public void UseMessageRetry_RejectsNullConfiguratorsConnectorsAndDelegates()
    {
        Action<IRetryConfigurator> configure = retry => retry.None();

        ArgumentNullException missingConfigurator = Assert.Throws<ArgumentNullException>(() =>
            MessageRetryConfigurationExtensions.UseMessageRetry((IConsumePipeConfigurator)null!, configure));
        ArgumentNullException missingConfiguration = Assert.Throws<ArgumentNullException>(() =>
            Bus.Factory.CreateUsingInMemory(bus =>
                MessageRetryConfigurationExtensions.UseMessageRetry((IConsumePipeConfigurator)bus, null!)));
        ArgumentNullException missingConnector = Assert.Throws<ArgumentNullException>(() =>
            Bus.Factory.CreateUsingInMemory(bus => MessageRetryConfigurationExtensions.UseMessageRetry(
                (IConsumePipeConfigurator)bus,
                null!,
                configure)));

        Assert.Equal("configurator", missingConfigurator.ParamName);
        Assert.Equal("configure", missingConfiguration.ParamName);
        Assert.Equal("connector", missingConnector.ParamName);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record ObservedRetryMessage;

    private sealed record RetryUntilThirdAttemptMessage;

    private sealed class ObservedRetryConsumer(RetryObservation observation) : IConsumer<ObservedRetryMessage>
    {
        public Task Consume(ConsumeContext<ObservedRetryMessage> context)
        {
            int count = observation.Record(context.GetRetryAttempt(), context.GetRetryCount());
            if (observation.AlwaysFail || count == 1)
                throw new ExpectedRetryException("retry requested");

            return Task.CompletedTask;
        }
    }

    private sealed class RetryUntilThirdAttemptConsumer(SuccessfulRetryObservation observation)
        : IConsumer<RetryUntilThirdAttemptMessage>
    {
        public Task Consume(ConsumeContext<RetryUntilThirdAttemptMessage> context)
        {
            if (observation.Record(context.GetRetryAttempt(), context.GetRetryCount()) < 3)
                throw new ExpectedRetryException("retry until the third attempt");

            observation.Completed.TrySetResult(true);
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessfulRetryObservation
    {
        private readonly object _lock = new();
        private readonly List<int> _attempts = [];
        private readonly List<int> _retryCounts = [];

        public TaskCompletionSource<bool> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int[] Attempts
        {
            get
            {
                lock (_lock)
                    return _attempts.ToArray();
            }
        }

        public int[] RetryCounts
        {
            get
            {
                lock (_lock)
                    return _retryCounts.ToArray();
            }
        }

        public int Record(int attempt, int retryCount)
        {
            lock (_lock)
            {
                _attempts.Add(attempt);
                _retryCounts.Add(retryCount);
                return _attempts.Count;
            }
        }
    }

    private sealed class RetryObservation(bool alwaysFail)
    {
        private readonly object _lock = new();
        private readonly List<int> _attempts = [];
        private readonly List<int> _retryCounts = [];

        public bool AlwaysFail { get; } = alwaysFail;

        public TaskCompletionSource<bool> FirstAttempt { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Count
        {
            get
            {
                lock (_lock)
                    return _attempts.Count;
            }
        }

        public int[] Attempts
        {
            get
            {
                lock (_lock)
                    return _attempts.ToArray();
            }
        }

        public int[] RetryCounts
        {
            get
            {
                lock (_lock)
                    return _retryCounts.ToArray();
            }
        }

        public int Record(int attempt, int retryCount)
        {
            int count;
            lock (_lock)
            {
                _attempts.Add(attempt);
                _retryCounts.Add(retryCount);
                count = _attempts.Count;
            }

            FirstAttempt.TrySetResult(true);
            return count;
        }
    }

    public interface IPolymorphicRetryContract
    {
        Guid CorrelationId { get; }
    }

    public abstract record AbstractPolymorphicRetryMessage(Guid CorrelationId) :
        IPolymorphicRetryContract;

    public record PolymorphicRetryBase(Guid CorrelationId) :
        AbstractPolymorphicRetryMessage(CorrelationId);

    public sealed record DerivedRetryMessage(Guid CorrelationId) :
        PolymorphicRetryBase(CorrelationId),
        IPolymorphicRetryContract;

    private sealed class PolymorphicRetryConsumer(PolymorphicRetryObservation observation) :
        IConsumer<PolymorphicRetryBase>,
        IConsumer<IPolymorphicRetryContract>
    {
        public Task Consume(ConsumeContext<PolymorphicRetryBase> context)
        {
            if (observation.RecordBase(context.Message.CorrelationId, context.GetRetryAttempt()) == 1)
                throw new ExpectedRetryException("retry base contract");

            observation.BaseCompleted.TrySetResult(true);
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<IPolymorphicRetryContract> context)
        {
            if (observation.RecordInterface(context.Message.CorrelationId, context.GetRetryAttempt()) == 1)
                throw new ExpectedRetryException("retry interface contract");

            observation.InterfaceCompleted.TrySetResult(true);
            return Task.CompletedTask;
        }
    }

    private sealed class PolymorphicRetryObservation
    {
        private readonly object _lock = new();
        private readonly List<int> _baseAttempts = [];
        private readonly List<int> _interfaceAttempts = [];

        public TaskCompletionSource<bool> BaseCompleted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> InterfaceCompleted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int BaseCount
        {
            get
            {
                lock (_lock)
                    return _baseAttempts.Count;
            }
        }

        public int InterfaceCount
        {
            get
            {
                lock (_lock)
                    return _interfaceAttempts.Count;
            }
        }

        public int[] BaseAttempts
        {
            get
            {
                lock (_lock)
                    return _baseAttempts.ToArray();
            }
        }

        public int[] InterfaceAttempts
        {
            get
            {
                lock (_lock)
                    return _interfaceAttempts.ToArray();
            }
        }

        public Guid BaseCorrelationId { get; private set; }
        public Guid InterfaceCorrelationId { get; private set; }

        public int RecordBase(Guid correlationId, int attempt)
        {
            lock (_lock)
            {
                BaseCorrelationId = correlationId;
                _baseAttempts.Add(attempt);
                return _baseAttempts.Count;
            }
        }

        public int RecordInterface(Guid correlationId, int attempt)
        {
            lock (_lock)
            {
                InterfaceCorrelationId = correlationId;
                _interfaceAttempts.Add(attempt);
                return _interfaceAttempts.Count;
            }
        }
    }

    private sealed class ExpectedRetryException(string message) : Exception(message);
}
