using System.Data.Common;
using System.Runtime.Serialization;
using System.Security;
using System.Text.Json;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class DefaultTechnicalFailureClassifierTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-FAILURE", "explicit-transient-and-terminal-taxonomy")]
    public void BuiltInTaxonomy_ClassifiesEveryOwnedFailureWithoutUsingMessageText()
    {
        var classifier = new DefaultTechnicalFailureClassifier();
        Exception[] transient =
        [
            new ConnectionException("permanent words do not matter", isTransient: true),
            new TransportUnavailableException("do not retry"),
            new TimeoutException("permanent"),
            new CircuitBreakerOpenException(TimeSpan.FromSeconds(1), probeInProgress: false, lastFailure: null),
            new ConcurrencyException("stale saga version", typeof(object), Guid.NewGuid()),
            new TestDbException(isTransient: true),
        ];
        Exception[] terminal =
        [
            new ConnectionException("temporary words do not matter", isTransient: false),
            new StaleConcurrencyLimitCommandException(DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddTicks(1)),
            new OperationCanceledException(),
            new ConfigurationException(),
            new PipeConfigurationException(),
            new MessageException(),
            new PayloadException(),
            new RoutingSlipArgumentException(),
            new DuplicateKeyPipeConfigurationException(),
            new UnknownStateException(),
            new UnknownEventException(),
            new UnhandledEventException(),
            new SerializationException(),
            new JsonException(),
            new SecurityException(),
            new UnauthorizedAccessException(),
            new ArgumentException(),
            new ObjectDisposedException("owner"),
            new InvalidOperationException(),
            new NotSupportedException(),
            new NotImplementedException(),
            new NullReferenceException(),
            new IndexOutOfRangeException(),
            new FormatException(),
            new OverflowException(),
        ];

        Assert.All(transient, failure => Assert.Equal(RetryFailureKind.Transient, classifier.Classify(failure)));
        Assert.All(terminal, failure => Assert.Equal(RetryFailureKind.NonRetryable, classifier.Classify(failure)));
        Assert.Equal(RetryFailureKind.Unclassified, classifier.Classify(new IOException("retry words do not matter")));
        Assert.Equal(RetryFailureKind.Unclassified, classifier.Classify(new TestDbException(isTransient: false)));
        Assert.Throws<ArgumentNullException>(() => classifier.Classify(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-FAILURE", "custom-inner-and-invalid-classification")]
    public void CustomAndNestedClassification_PreservesOwnedValuesAndFailsClosedForInvalidValues()
    {
        var classifier = new DefaultTechnicalFailureClassifier();
        var nested = new Exception("outer is unclassified", new ClassifiedException(RetryFailureKind.Transient));

        Assert.Equal(RetryFailureKind.Transient, classifier.Classify(nested));
        Assert.Equal(
            RetryFailureKind.NonRetryable,
            classifier.Classify(new ConnectionException("outer is transient", new ArgumentException("inner is terminal"), true)));
        Assert.Equal(
            RetryFailureKind.NonRetryable,
            classifier.Classify(new ConcurrencyException("outer is transient", typeof(object), Guid.NewGuid(), new ArgumentException("inner is terminal"))));
        Assert.Equal(
            RetryFailureKind.Transient,
            classifier.Classify(new InvalidOperationException("provider wrapper", new TestDbException(isTransient: true))));
        Assert.Equal(
            RetryFailureKind.NonRetryable,
            classifier.Classify(new InvalidOperationException("programming failure", new TestDbException(isTransient: false))));
        Assert.Equal(RetryFailureKind.NonRetryable, classifier.Classify(new ClassifiedException(RetryFailureKind.NonRetryable)));
        Assert.Equal(RetryFailureKind.Unclassified, classifier.Classify(new ClassifiedException(RetryFailureKind.Unclassified)));
        Assert.Equal(RetryFailureKind.Unclassified, classifier.Classify(new ClassifiedException((RetryFailureKind)937)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TECHNICAL-FAILURE", "aggregate-precedence-is-order-independent")]
    public void AggregateClassification_IsOrderIndependentAndTerminalEvidenceAlwaysDominates()
    {
        var classifier = new DefaultTechnicalFailureClassifier();
        var transient = new TimeoutException();
        var terminal = new ArgumentException();
        var unknown = new IOException();

        Assert.Equal(RetryFailureKind.Transient, classifier.Classify(new AggregateException(transient, new TransportUnavailableException())));
        Assert.Equal(RetryFailureKind.Unclassified, classifier.Classify(new AggregateException(transient, unknown)));
        Assert.Equal(RetryFailureKind.Unclassified, classifier.Classify(new AggregateException(unknown, transient)));
        Assert.Equal(RetryFailureKind.NonRetryable, classifier.Classify(new AggregateException(unknown, terminal, transient)));
        Assert.Equal(RetryFailureKind.NonRetryable, classifier.Classify(new AggregateException(transient, terminal, unknown)));
        Assert.Equal(RetryFailureKind.Unclassified, classifier.Classify(new AggregateException()));
    }

    private sealed class ClassifiedException(RetryFailureKind kind) : Exception, IRetryFailureClassification
    {
        public RetryFailureKind RetryFailureKind { get; } = kind;
    }

    private sealed class TestDbException(bool isTransient) : DbException("test-owned database failure")
    {
        public override bool IsTransient { get; } = isTransient;
    }
}
