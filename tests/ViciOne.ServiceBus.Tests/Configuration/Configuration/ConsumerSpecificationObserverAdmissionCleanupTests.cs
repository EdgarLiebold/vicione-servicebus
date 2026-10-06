using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class ConsumerSpecificationObserverAdmissionCleanupTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONFIGURATION-OBSERVATION", "public-observer-admission-cleanup-and-primary-causes")]
    public void ConsumerSpecification_ObserverAdmissionRetiresReturnedHandlesAndPreservesCauses(int failureMode)
    {
        var state = new AdmissionState(failureMode);
        var alpha = new MessageSpecificationFacade<AlphaMessage>(state);
        var beta = new MessageSpecificationFacade<BetaMessage>(state);
        var gamma = new MessageSpecificationFacade<GammaMessage>(state);
        IConsumerMessageSpecification<ProbeConsumer>[] specifications = [alpha, beta, gamma];
        ConsumerSpecification<ProbeConsumer>? subject = null;
        ConnectHandle? externalHandle = null;

        try
        {
            Exception? failure = Record.Exception(() =>
            {
                subject = new ConsumerSpecification<ProbeConsumer>(specifications);
            });

            Assert.Equal(3, state.ConnectCalls);
            if (failureMode == 0)
            {
                Assert.Null(failure);
                Assert.NotNull(subject);
                Assert.Equal(3, state.Acquired.Count);
                Assert.Equal(0, state.AdmissionThrowCount);
                Assert.All(state.Acquired, handle =>
                {
                    Assert.Equal(1, handle.Observers.Count);
                    Assert.Equal(0, handle.ReleaseAttempts);
                });
                Assert.All(state.CapturedObservers, observer => Assert.Same(state.CapturedObservers[0], observer));

                var probe = new RecordingObserver();
                externalHandle = subject.ConnectConsumerConfigurationObserver(probe);
                Assert.Same(alpha.Actual, subject.GetMessageSpecification<AlphaMessage>());
                Assert.Same(beta.Actual, subject.GetMessageSpecification<BetaMessage>());
                Assert.Same(gamma.Actual, subject.GetMessageSpecification<GammaMessage>());
                Assert.Empty(subject.Validate());
                Assert.Equal(typeof(ProbeConsumer), Assert.Single(probe.ConsumerTypes));
                Assert.Same(subject, Assert.Single(probe.ConsumerConfigurations));
                Assert.Equal(3, probe.MessageConfigurations.Count);
                Assert.All(probe.MessageConfigurations, value => Assert.Equal(typeof(ProbeConsumer), value.ConsumerType));
                Assert.Same(alpha.Actual, Assert.Single(probe.MessageConfigurations, value => value.MessageType == typeof(AlphaMessage)).Configuration);
                Assert.Same(beta.Actual, Assert.Single(probe.MessageConfigurations, value => value.MessageType == typeof(BetaMessage)).Configuration);
                Assert.Same(gamma.Actual, Assert.Single(probe.MessageConfigurations, value => value.MessageType == typeof(GammaMessage)).Configuration);
                Assert.All(state.Acquired, handle => Assert.Equal(0, handle.ReleaseAttempts));
            }
            else
            {
                Assert.Null(subject);
                Assert.NotNull(failure);
                Assert.Equal(1, state.AdmissionThrowCount);
                Assert.Same(state.AdmissionFailure, state.ThrownAdmissionFailure);
                Assert.Equal(2, state.Acquired.Count);
                Assert.Equal(3, state.CapturedObservers.Count);
                Assert.All(state.CapturedObservers, observer => Assert.Same(state.CapturedObservers[0], observer));

                // Count is the real public registration state before fixture fallback cleanup.
                Assert.Equal(0, state.Acquired[0].Observers.Count);
                Assert.Equal(0, state.Acquired[1].Observers.Count);
                Assert.All(state.Acquired, handle => Assert.Equal(1, handle.ReleaseAttempts));
                if (failureMode == 1)
                {
                    Assert.Same(state.AdmissionFailure, failure);
                    Assert.Equal(0, state.CleanupThrowCount);
                }
                else
                {
                    Assert.Equal(1, state.CleanupThrowCount);
                    Assert.Same(state.CleanupFailure, state.ThrownCleanupFailure);
                    Exception[] causes = EnumerateCauses(failure).ToArray();
                    Assert.Single(causes, cause => ReferenceEquals(cause, state.AdmissionFailure));
                    Assert.Single(causes, cause => ReferenceEquals(cause, state.CleanupFailure));
                }
            }
        }
        finally
        {
            try
            {
                // Bypass only our injected release wrapper; independently retire every real handle.
                var failures = new List<Exception>();
                foreach (ReturnedHandle handle in state.Acquired)
                {
                    try
                    {
                        handle.Inner.Disconnect();
                    }
                    catch (Exception exception)
                    {
                        failures.Add(exception);
                    }
                }
                if (failures.Count != 0)
                    throw new AggregateException(failures);
            }
            finally
            {
                externalHandle?.Disconnect();
            }
        }
    }

    private static IEnumerable<Exception> EnumerateCauses(Exception exception)
    {
        yield return exception;
        if (exception is AggregateException aggregate)
        {
            foreach (Exception inner in aggregate.InnerExceptions)
            foreach (Exception cause in EnumerateCauses(inner))
                yield return cause;
        }
        else if (exception.InnerException is { } inner)
        {
            foreach (Exception cause in EnumerateCauses(inner))
                yield return cause;
        }
    }

    private sealed class AdmissionState(int failureMode)
    {
        public int FailureMode { get; } = failureMode;
        public IOException AdmissionFailure { get; } = new("observer-admission-primary");
        public ApplicationException CleanupFailure { get; } = new("observer-release-secondary");
        public int ConnectCalls { get; set; }
        public int AdmissionThrowCount { get; set; }
        public int CleanupThrowCount { get; set; }
        public Exception? ThrownAdmissionFailure { get; set; }
        public Exception? ThrownCleanupFailure { get; set; }
        public List<IConsumerConfigurationObserver> CapturedObservers { get; } = [];
        public List<ReturnedHandle> Acquired { get; } = [];
    }

    private sealed class MessageSpecificationFacade<TMessage>(AdmissionState state) :
        IConsumerMessageSpecification<ProbeConsumer>
        where TMessage : class
    {
        public ConsumerMessageSpecification<ProbeConsumer, TMessage> Actual { get; } = new();
        public ConsumerConfigurationObservable Observers { get; } = new();
        public Type MessageType => Actual.MessageType;

        public bool TryGetMessageSpecification<TRequestedConsumer, TRequestedMessage>(
            [NotNullWhen(true)] out IConsumerMessageSpecification<TRequestedConsumer, TRequestedMessage>? specification)
            where TRequestedMessage : class
            where TRequestedConsumer : class => Actual.TryGetMessageSpecification(out specification);

        public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<ProbeConsumer>> specification) =>
            Actual.AddPipeSpecification(specification);

        public IEnumerable<ValidationResult> Validate()
        {
            Observers.ConsumerMessageConfigured(Actual);
            return Actual.Validate();
        }

        public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            state.ConnectCalls++;
            state.CapturedObservers.Add(observer);
            if (state.FailureMode != 0 && state.ConnectCalls == 3)
            {
                state.AdmissionThrowCount++;
                state.ThrownAdmissionFailure = state.AdmissionFailure;
                throw state.AdmissionFailure;
            }

            ConnectHandle inner = Observers.Connect(observer);
            var handle = new ReturnedHandle(inner, Observers, state,
                state.FailureMode == 2 && state.Acquired.Count == 0);
            state.Acquired.Add(handle);
            return handle;
        }
    }

    private sealed class ReturnedHandle(
        ConnectHandle inner,
        ConsumerConfigurationObservable observers,
        AdmissionState state,
        bool throwCleanup) : ConnectHandle
    {
        public ConnectHandle Inner { get; } = inner;
        public ConsumerConfigurationObservable Observers { get; } = observers;
        public int ReleaseAttempts { get; private set; }

        public void Dispose() => Disconnect();

        public void Disconnect()
        {
            ReleaseAttempts++;
            Inner.Disconnect();
            if (throwCleanup && ReleaseAttempts == 1)
            {
                state.CleanupThrowCount++;
                state.ThrownCleanupFailure = state.CleanupFailure;
                throw state.CleanupFailure;
            }
        }
    }

    private sealed class RecordingObserver : IConsumerConfigurationObserver
    {
        public List<Type> ConsumerTypes { get; } = [];
        public List<object> ConsumerConfigurations { get; } = [];
        public List<(Type ConsumerType, Type MessageType, object Configuration)> MessageConfigurations { get; } = [];

        public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
            where TConsumer : class
        {
            ConsumerTypes.Add(typeof(TConsumer));
            ConsumerConfigurations.Add(configurator);
        }

        public void ConsumerMessageConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
            where TConsumer : class
            where TMessage : class => MessageConfigurations.Add((typeof(TConsumer), typeof(TMessage), configurator));
    }

    public sealed record AlphaMessage;
    public sealed record BetaMessage;
    public sealed record GammaMessage;

    public sealed class ProbeConsumer : IConsumer<AlphaMessage>, IConsumer<BetaMessage>, IConsumer<GammaMessage>
    {
        public Task ConsumeAsync(ConsumeContext<AlphaMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<BetaMessage> context) => Task.CompletedTask;
        public Task ConsumeAsync(ConsumeContext<GammaMessage> context) => Task.CompletedTask;
    }
}
