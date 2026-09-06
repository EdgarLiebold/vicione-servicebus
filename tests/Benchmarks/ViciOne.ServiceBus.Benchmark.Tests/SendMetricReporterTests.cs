using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOneServiceBusBenchmark.BusOutbox;
using ViciOneServiceBusBenchmark.Latency;
using Xunit;

namespace ViciOne.ServiceBus.Benchmark.Tests;

public sealed class SendMetricReporterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-REPORTER", "message-id-is-required")]
    public async Task A_send_without_a_message_id_is_refusedAsync()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendMetricReporter.ReportAsync(new MessageMetricCapture(1), null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-REPORTER", "unregistered-completion-surfaces")]
    public async Task An_unregistered_completion_surfaces_through_the_reporterAsync()
    {
        var capture = new MessageMetricCapture(1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendMetricReporter.ReportAsync(capture, Guid.NewGuid()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-REPORTER", "faulted-task-is-propagated")]
    public async Task A_faulted_capture_task_is_not_swallowedAsync()
    {
        var expected = new InvalidOperationException("the capture refused this completion");

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SendMetricReporter.ReportAsync(new FaultingMetric(expected), Guid.NewGuid()));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-REPORTER", "registered-message-completed")]
    public async Task A_registered_message_is_completed_through_the_reporterAsync()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.SentAsync(messageId, () => SendMetricReporter.ReportAsync(capture, messageId), true);

        Assert.True(capture.SendCompleted.IsCompletedSuccessfully);
    }

    private sealed class FaultingMetric(Exception exception) : IReportConsumerMetric
    {
        public Task ConsumedAsync<T>(Guid messageId) where T : class => Task.CompletedTask;

        public Task SentAsync(Guid messageId, Func<Task> send, bool postSend = false) => Task.CompletedTask;

        public Task PostSendAsync(Guid messageId) => Task.FromException(exception);
    }
}
