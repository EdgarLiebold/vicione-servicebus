using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Introspection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class TechnicalRetryPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-RETRY", "immutable-canonical-schedules")]
    public void CanonicalSchedules_AreExactReadOnlySnapshotsWithOneDefaultClassifier()
    {
        Assert.Equal(
        [
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(2),
        ], TechnicalRetryPolicy.ImmediateIntervals);
        Assert.Equal(
        [
            TimeSpan.FromSeconds(15),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
        ], TechnicalRetryPolicy.RedeliveryIntervals);
        Assert.Same(TechnicalRetryPolicy.DefaultFailureClassifier, TechnicalRetryPolicy.DefaultFailureClassifier);

        var immediate = Assert.IsAssignableFrom<IList<TimeSpan>>(TechnicalRetryPolicy.ImmediateIntervals);
        var redelivery = Assert.IsAssignableFrom<IList<TimeSpan>>(TechnicalRetryPolicy.RedeliveryIntervals);
        Assert.True(immediate.IsReadOnly);
        Assert.True(redelivery.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => immediate[0] = TimeSpan.Zero);
        Assert.Throws<NotSupportedException>(() => redelivery[0] = TimeSpan.Zero);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-RETRY", "immediate-policy-filters-and-delays")]
    public void ImmediatePolicy_UsesTheCustomClassifierAndEveryDelayExactlyOnce()
    {
        var configurator = new CapturingRedeliveryConfigurator();
        var classifier = new RecordingClassifier();
        var transient = new ClassifiedTestException(RetryFailureKind.Transient);
        var terminal = new ClassifiedTestException(RetryFailureKind.NonRetryable);
        var unknown = new ClassifiedTestException(RetryFailureKind.Unclassified);

        TechnicalRetryPolicy.ConfigureImmediate(configurator, classifier);
        IRetryPolicy policy = configurator.Build();

        AssertPolicy(policy, transient, TechnicalRetryPolicy.ImmediateIntervals);
        AssertNoRetry(policy, terminal);
        AssertNoRetry(policy, unknown);
        Assert.Equal([transient, transient, transient, terminal, unknown], classifier.Observed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-RETRY", "redelivery-policy-filters-and-delays")]
    public void RedeliveryPolicy_UsesTheDefaultClassifierAndEveryDelayExactlyOnce()
    {
        var configurator = new CapturingRedeliveryConfigurator();

        TechnicalRetryPolicy.ConfigureRedelivery(configurator);
        IRetryPolicy policy = configurator.Build();

        AssertPolicy(policy, new TimeoutException(), TechnicalRetryPolicy.RedeliveryIntervals);
        AssertNoRetry(policy, new ArgumentException());
        AssertNoRetry(policy, new IOException());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-RETRY", "configuration-null-boundaries")]
    public void ConfigurationEntryPoints_RejectNullOwnersBeforeAddingSpecifications()
    {
        Assert.Throws<ArgumentNullException>(() => TechnicalRetryPolicy.ConfigureImmediate(null!));
        Assert.Throws<ArgumentNullException>(() => TechnicalRetryPolicy.ConfigureRedelivery(null!));
        Assert.Throws<ArgumentNullException>(() => TechnicalRetryConfigurationExtensions.UseTechnicalMessageRetry(null!));
        Assert.Throws<ArgumentNullException>(() => TechnicalRetryConfigurationExtensions.UseTechnicalDelayedRedelivery(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-RETRY", "public-extensions-bind-canonical-policies")]
    public void PublicExtensions_BindTheCanonicalPoliciesToEveryDiscoveredMessageType()
    {
        IDictionary<string, object> immediate = PolicyProbe(
            configurator => configurator.UseTechnicalMessageRetry());
        IDictionary<string, object> redelivery = PolicyProbe(
            configurator => configurator.UseTechnicalDelayedRedelivery());

        AssertPolicyProbe(immediate, TechnicalRetryPolicy.ImmediateIntervals);
        AssertPolicyProbe(redelivery, TechnicalRetryPolicy.RedeliveryIntervals);
    }

    static void AssertPolicy(IRetryPolicy policy, Exception failure, IReadOnlyList<TimeSpan> expectedDelays)
    {
        using RetryPolicyContext<TestPipeContext> context = policy.CreatePolicyContext(new TestPipeContext());

        Assert.True(context.CanRetry(failure, out RetryContext<TestPipeContext> retry));
        Assert.Equal(expectedDelays[0], retry.Delay);
        for (var index = 1; index < expectedDelays.Count; index++)
        {
            Assert.True(retry.CanRetry(failure, out RetryContext<TestPipeContext> next));
            Assert.Equal(expectedDelays[index], next.Delay);
            retry = next;
        }

        Assert.False(retry.CanRetry(failure, out RetryContext<TestPipeContext> terminal));
        Assert.Equal(expectedDelays.Count, terminal.RetryCount);
    }

    static void AssertNoRetry(IRetryPolicy policy, Exception failure)
    {
        using RetryPolicyContext<TestPipeContext> context = policy.CreatePolicyContext(new TestPipeContext());

        Assert.False(context.CanRetry(failure, out RetryContext<TestPipeContext> terminal));
        Assert.Equal(0, terminal.RetryCount);
    }

    private static IDictionary<string, object> PolicyProbe(Action<ConsumePipeSpecification> configure)
    {
        var configurator = new ConsumePipeSpecification();
        configure(configurator);
        var handler = new HandlerConfigurator<PolicyMessage>(_ => Task.CompletedTask, configurator);
        Assert.Empty(handler.Validate());
        IConsumePipe pipe = configurator.BuildConsumePipe();
        using ConnectHandle handle = pipe.ConnectConsumePipe<PolicyMessage>(Pipe.Empty<ConsumeContext<PolicyMessage>>());

        IProbeResult probe = pipe.GetProbeResult();
        return Assert.Single(Descendants(probe.Results),
            candidate => candidate.TryGetValue("policy", out object? policy) && Equals(policy, "Interval"));
    }

    private static void AssertPolicyProbe(IDictionary<string, object> probe, IReadOnlyList<TimeSpan> expectedIntervals)
    {
        Assert.Equal(expectedIntervals.Count, Assert.IsType<int>(Assert.Contains("limit", probe)));
        Assert.Equal(
            expectedIntervals,
            Assert.IsAssignableFrom<IEnumerable<TimeSpan>>(Assert.Contains("intervals", probe)).ToArray());
    }

    private static IEnumerable<IDictionary<string, object>> Descendants(object value)
    {
        if (value is IDictionary<string, object> dictionary)
        {
            yield return dictionary;
            foreach (object child in dictionary.Values)
            {
                foreach (IDictionary<string, object> descendant in Descendants(child))
                    yield return descendant;
            }
        }
        else if (value is IEnumerable<object> sequence)
        {
            foreach (object child in sequence)
            {
                foreach (IDictionary<string, object> descendant in Descendants(child))
                    yield return descendant;
            }
        }
    }

    private sealed class CapturingRedeliveryConfigurator : ExceptionSpecification, IRedeliveryConfigurator
    {
        RetryPolicyFactory? _factory;

        public bool ReplaceMessageId { private get; set; }

        public void SetRetryPolicy(RetryPolicyFactory factory) => _factory = factory;

        public ConnectHandle ConnectRetryObserver(IRetryObserver observer) => new EmptyConnectHandle();

        public IRetryPolicy Build()
        {
            Assert.NotNull(_factory);
            return _factory(CreateFilterSnapshot());
        }
    }

    private sealed class RecordingClassifier : ITechnicalFailureClassifier
    {
        public List<Exception> Observed { get; } = [];

        public RetryFailureKind Classify(Exception exception)
        {
            Observed.Add(exception);
            return ((ClassifiedTestException)exception).Kind;
        }
    }

    private sealed class ClassifiedTestException(RetryFailureKind kind) : Exception
    {
        public RetryFailureKind Kind { get; } = kind;
    }

    private sealed class TestPipeContext : BasePipeContext;

    private sealed record PolicyMessage;
}
