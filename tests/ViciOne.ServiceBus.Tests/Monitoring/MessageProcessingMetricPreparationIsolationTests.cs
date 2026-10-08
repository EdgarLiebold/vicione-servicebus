using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class MessageProcessingMetricPreparationIsolationTests
{
    [Theory]
    [InlineData(DiagnosticFailure.None, false)]
    [InlineData(DiagnosticFailure.None, true)]
    [InlineData(DiagnosticFailure.UtcNow, false)]
    [InlineData(DiagnosticFailure.UtcNow, true)]
    [InlineData(DiagnosticFailure.SentTime, false)]
    [InlineData(DiagnosticFailure.SentTime, true)]
    [InlineData(DiagnosticFailure.RetryAttempt, false)]
    [InlineData(DiagnosticFailure.RetryAttempt, true)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "process-metric-balance-survives-optional-preparation")]
    public async Task Handler_PreservesOutcomeAndIndependentMetricsWhenDiagnosticPreparationThrowsAsync(
        DiagnosticFailure diagnosticFailure, bool businessFailure)
    {
        var start = new DateTimeOffset(2045, 6, 7, 8, 9, 10, TimeSpan.Zero);
        var clock = new DiagnosticClock(start, diagnosticFailure == DiagnosticFailure.UtcNow);
        var item = new MetricItem("real handler member");
        ConsumeContext<MetricItem> inner = InMemoryOutboxTestContextFactory.Create(
            item, TestContext.Current.CancellationToken, sentTime: start.AddSeconds(-3));
        inner.SetTimeProvider(clock);
        inner.GetOrAddPayload<ConsumeRetryContext>(() => new RetryMarker(diagnosticFailure == DiagnosticFailure.RetryAttempt));
        var context = new ObservedContext(inner, diagnosticFailure == DiagnosticFailure.SentTime);
        var requiredFailure = new InvalidOperationException("required handler failed");
        var next = new NextPipe();
        using ServiceProvider provider = new ServiceCollection().AddLogging().AddMetrics().BuildServiceProvider();
        using var observations = new MetricObservationSession(provider.GetRequiredService<IMeterFactory>());
        MetricMeasurement[]? activeDuringHandler = null;
        var handlerCalls = 0;
        IFilter<ConsumeContext<MetricItem>> filter = new HandlerMessageFilter<MetricItem>(received =>
        {
            Assert.Same(context, received);
            Assert.Same(item, received.Message);
            handlerCalls++;
            activeDuringHandler = observations.Measurements
                .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations).ToArray();
            clock.Advance(TimeSpan.FromSeconds(7));
            return businessFailure ? Task.FromException(requiredFailure) : Task.CompletedTask;
        });
        ILogContext? previous = LogContext.Current;
        Exception? escaped;
        try
        {
            LogContext.Current = null;
            LogContext.ConfigureCurrentLogContextIfNull(provider);
            escaped = await Record.ExceptionAsync(() => filter.SendAsync(context, next));
        }
        finally
        {
            LogContext.Current = previous;
        }

        if (businessFailure)
            Assert.Same(requiredFailure, escaped);
        else
            Assert.Null(escaped);
        Assert.Equal(1, handlerCalls);
        Assert.Equal(businessFailure ? 0 : 1, next.Calls);
        Assert.Equal(businessFailure ? 0 : 1, context.Consumed);
        Assert.Equal(businessFailure ? 1 : 0, context.Faulted);
        Assert.Equal(TimeSpan.FromSeconds(7), context.NotificationDuration);
        Assert.Same(businessFailure ? requiredFailure : null, context.NotificationFailure);
        if (!businessFailure)
            Assert.Same(context, next.Context);

        Assert.NotNull(activeDuringHandler);
        MetricMeasurement during = Assert.Single(activeDuringHandler);
        Assert.Equal(1d, during.Value);
        AssertProcessTags(during, error: false);

        MetricMeasurement[] active = observations.Measurements
            .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.ActiveOperations).ToArray();
        Assert.Equal(new[] { 1d, -1d }, active.Select(measurement => measurement.Value));
        Assert.Equal(0d, active.Sum(measurement => measurement.Value));
        Assert.All(active, measurement => AssertProcessTags(measurement, error: false));
        MetricMeasurement duration = Assert.Single(observations.Measurements,
            measurement => measurement.Name == ServiceBusTelemetry.Metrics.ProcessDuration);
        Assert.Equal(7d, duration.Value);
        AssertProcessTags(duration, error: businessFailure);
        MetricMeasurement[] delivery = observations.Measurements
            .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.DeliveryDuration).ToArray();
        if (diagnosticFailure is DiagnosticFailure.None or DiagnosticFailure.RetryAttempt)
        {
            MetricMeasurement lag = Assert.Single(delivery);
            Assert.Equal(3d, lag.Value);
            AssertProcessTags(lag, error: false);
        }
        else
            Assert.Empty(delivery);

        MetricMeasurement[] retries = observations.Measurements
            .Where(measurement => measurement.Name == ServiceBusTelemetry.Metrics.RetryAttempts).ToArray();
        if (diagnosticFailure == DiagnosticFailure.RetryAttempt)
            Assert.Empty(retries);
        else
        {
            MetricMeasurement retry = Assert.Single(retries);
            Assert.Equal(1d, retry.Value);
            AssertProcessTags(retry, error: false);
        }
    }

    private static void AssertProcessTags(MetricMeasurement measurement, bool error)
    {
        string[] expected =
        [
            ServiceBusTelemetry.Attributes.MessagingSystem,
            ServiceBusTelemetry.Attributes.OperationName,
            ServiceBusTelemetry.Attributes.OperationType,
            ServiceBusTelemetry.Attributes.ProcessorKind,
        ];
        if (error)
            expected = [.. expected, ServiceBusTelemetry.Attributes.ErrorType];
        Assert.Equal(expected.Order(), measurement.Tags.Select(tag => tag.Key).Order());
        Assert.Equal(ServiceBusTelemetry.MessagingSystems.InMemory, measurement.Tag(ServiceBusTelemetry.Attributes.MessagingSystem));
        Assert.Equal("handle", measurement.Tag(ServiceBusTelemetry.Attributes.OperationName));
        Assert.Equal("process", measurement.Tag(ServiceBusTelemetry.Attributes.OperationType));
        Assert.Equal("handler", measurement.Tag(ServiceBusTelemetry.Attributes.ProcessorKind));
        if (error)
            Assert.Equal(typeof(InvalidOperationException).FullName, measurement.Tag(ServiceBusTelemetry.Attributes.ErrorType));
    }

    public enum DiagnosticFailure
    {
        None,
        UtcNow,
        SentTime,
        RetryAttempt,
    }

    public sealed record MetricItem(string Value);

    private sealed class RetryMarker(bool hostileRetry) : ConsumeRetryContext
    {
        public int RetryAttempt => hostileRetry
            ? throw new InvalidOperationException("optional retry-attempt read failed")
            : 1;
        public int RetryCount => 0;
        public TContext CreateNext<TContext>(RetryContext retryContext) where TContext : class, ConsumeRetryContext =>
            throw new NotSupportedException("The fixture does not perform a real retry.");
        public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class DiagnosticClock(DateTimeOffset start, bool hostileUtc) : TimeProvider
    {
        private readonly FakeTimeProvider _inner = new(start);

        public override long TimestampFrequency => _inner.TimestampFrequency;
        public override long GetTimestamp() => _inner.GetTimestamp();
        public override DateTimeOffset GetUtcNow() => hostileUtc
            ? throw new InvalidOperationException("optional delivery-lag UTC read failed")
            : _inner.GetUtcNow();
        public void Advance(TimeSpan elapsed) => _inner.Advance(elapsed);
    }

    private sealed class ObservedContext(ConsumeContext<MetricItem> inner, bool hostileSentTime)
        : ConsumeContextProxy<MetricItem>(inner)
    {
        public int Consumed { get; private set; }
        public int Faulted { get; private set; }
        public TimeSpan? NotificationDuration { get; private set; }
        public Exception? NotificationFailure { get; private set; }

        public override DateTimeOffset? SentTime => hostileSentTime
            ? throw new InvalidOperationException("optional delivery-lag SentTime read failed")
            : base.SentTime;

        public override Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType,
            CancellationToken cancellationToken = default)
        {
            Assert.Same(this, context);
            Consumed++;
            NotificationDuration = duration;
            return Task.CompletedTask;
        }

        public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType,
            Exception exception, CancellationToken cancellationToken = default)
        {
            Assert.Same(this, context);
            Faulted++;
            NotificationDuration = duration;
            NotificationFailure = exception;
            return Task.CompletedTask;
        }
    }

    private sealed class NextPipe : IPipe<ConsumeContext<MetricItem>>
    {
        public int Calls { get; private set; }
        public ConsumeContext<MetricItem>? Context { get; private set; }
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(ConsumeContext<MetricItem> context)
        {
            Calls++;
            Context = context;
            return Task.CompletedTask;
        }
    }
}
