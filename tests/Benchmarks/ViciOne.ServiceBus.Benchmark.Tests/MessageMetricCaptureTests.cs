using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOneServiceBusBenchmark.BusOutbox;
using ViciOneServiceBusBenchmark.Latency;
using Xunit;

namespace ViciOne.ServiceBus.Benchmark.Tests;

public sealed class MessageMetricCaptureTests
{
    private static IReportConsumerMetric Reporting(MessageMetricCapture capture) => capture;

    private static Task ConsumeAsync(MessageMetricCapture capture, Guid messageId) =>
        Reporting(capture).ConsumedAsync<object>(messageId);

    private static MessageMetric SingleMetric(MessageMetricCapture capture) =>
        Assert.Single(capture.GetMessageMetrics());

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "send-start-registered-before-delegate")]
    public async Task The_start_is_registered_before_the_send_delegate_runsAsync()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.SentAsync(messageId, async () =>
        {
            await Task.Yield();
            await capture.PostSendAsync(messageId);
        }, true);
        await ConsumeAsync(capture, messageId);

        Assert.Single(capture.GetMessageMetrics());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "completion-followed-by-failure-not-counted")]
    public async Task A_send_that_reports_completion_and_then_fails_is_not_countedAsync()
    {
        var capture = new MessageMetricCapture(2);
        var failing = Guid.NewGuid();
        var succeeding = Guid.NewGuid();
        var expected = new InvalidOperationException("the send failed after the observer reported it");

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            capture.SentAsync(failing, async () =>
            {
                await capture.PostSendAsync(failing);
                throw expected;
            }, true));
        await capture.SentAsync(succeeding, () => capture.PostSendAsync(succeeding), true);

        Assert.Same(expected, actual);
        Assert.False(capture.SendCompleted.IsCompleted,
            "One confirmed send out of two must not finish the series.");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "completion-before-return-counted-once")]
    public async Task A_completion_reported_before_the_delegate_returns_is_counted_onceAsync()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.SentAsync(messageId, () => capture.PostSendAsync(messageId), true);

        Assert.True(capture.SendCompleted.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "completion-after-return-counted-once")]
    public async Task A_completion_reported_after_the_delegate_returns_is_counted_onceAsync()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.SentAsync(messageId, () => Task.CompletedTask, true);
        Assert.False(capture.SendCompleted.IsCompleted);

        await capture.PostSendAsync(messageId);

        Assert.True(capture.SendCompleted.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "failure-before-completion-is-terminal")]
    public async Task A_failure_before_any_completion_is_terminalAsync()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();
        var expected = new InvalidOperationException("no completion");

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => capture.SentAsync(messageId, () => throw expected, true));

        Assert.Same(expected, actual);
        Assert.False(capture.SendCompleted.IsCompleted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.PostSendAsync(messageId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "failed-message-id-can-be-retried")]
    public async Task A_message_id_can_be_used_again_after_a_failed_sendAsync()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.SentAsync(messageId, async () =>
        {
            await capture.PostSendAsync(messageId);
            throw new InvalidOperationException("first attempt failed");
        }, true));

        await capture.SentAsync(messageId, () => capture.PostSendAsync(messageId), true);

        Assert.True(capture.SendCompleted.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "unregistered-completion-rejected")]
    public async Task An_unregistered_completion_is_refused_rather_than_inventedAsync()
    {
        var capture = new MessageMetricCapture(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.PostSendAsync(Guid.NewGuid()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "unregistered-completion-does-not-advance")]
    public async Task An_unregistered_completion_does_not_advance_the_send_countAsync()
    {
        var capture = new MessageMetricCapture(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => capture.PostSendAsync(Guid.NewGuid()));

        Assert.False(capture.SendCompleted.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "duplicate-completion-counts-once")]
    public async Task A_repeated_completion_counts_onceAsync()
    {
        var capture = new MessageMetricCapture(2);
        var first = Guid.NewGuid();

        await capture.SentAsync(first, async () =>
        {
            await capture.PostSendAsync(first);
            await capture.PostSendAsync(first);
            await capture.PostSendAsync(first);
        }, true);

        Assert.False(capture.SendCompleted.IsCompleted,
            "Three completions of one message must not finish a series of two.");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "duplicate-completion-keeps-first-timestamp")]
    public async Task A_repeated_completion_keeps_the_first_timestampAsync()
    {
        var clock = new BenchmarkTestClock();
        var capture = new MessageMetricCapture(1, clock);
        var messageId = Guid.NewGuid();

        await capture.SentAsync(messageId, () =>
        {
            clock.Advance(17);
            return capture.PostSendAsync(messageId);
        }, true);
        clock.Advance(5);
        await ConsumeAsync(capture, messageId);
        long first = SingleMetric(capture).SendCompletionLatency;

        clock.Advance(10_000);
        await capture.PostSendAsync(messageId);

        Assert.Equal(17, first);
        Assert.Equal(first, SingleMetric(capture).SendCompletionLatency);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "duplicate-completion-does-not-add-sample")]
    public async Task A_repeated_completion_does_not_add_a_sampleAsync()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.SentAsync(messageId, () => capture.PostSendAsync(messageId), true);
        await capture.PostSendAsync(messageId);
        await ConsumeAsync(capture, messageId);

        Assert.Single(capture.GetMessageMetrics());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "failed-send-removes-registration")]
    public async Task A_failed_send_leaves_no_pending_measurementAsync()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => capture.SentAsync(messageId, () => throw new InvalidOperationException("send failed")));

        await capture.SentAsync(messageId, () => Task.CompletedTask);
        Assert.True(capture.SendCompleted.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "failed-send-not-counted")]
    public async Task A_failed_send_is_not_counted_as_sentAsync()
    {
        var capture = new MessageMetricCapture(1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => capture.SentAsync(Guid.NewGuid(), () => throw new InvalidOperationException("no")));

        Assert.False(capture.SendCompleted.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "duplicate-registration-rejected")]
    public async Task Registering_the_same_message_twice_is_refusedAsync()
    {
        var capture = new MessageMetricCapture(2);
        var messageId = Guid.NewGuid();

        await capture.SentAsync(messageId, () => Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => capture.SentAsync(messageId, () => Task.CompletedTask));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "direct-mode-completes-after-delegate")]
    public async Task The_direct_mode_still_completes_its_own_measurementAsync()
    {
        var clock = new BenchmarkTestClock();
        var capture = new MessageMetricCapture(1, clock);
        var messageId = Guid.NewGuid();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task sending = capture.SentAsync(messageId, async () =>
        {
            entered.SetResult();
            await release.Task;
            clock.Advance(31);
        });

        await entered.Task;
        Assert.False(capture.SendCompleted.IsCompleted);
        release.SetResult();
        await sending;
        clock.Advance(7);
        await ConsumeAsync(capture, messageId);

        Assert.True(capture.SendCompleted.IsCompletedSuccessfully);
        Assert.Equal(31, SingleMetric(capture).SendCompletionLatency);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "incomplete-send-has-no-sample")]
    public async Task An_incomplete_send_contributes_no_sampleAsync()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.SentAsync(messageId, () => Task.CompletedTask, true);
        await ConsumeAsync(capture, messageId);

        Assert.Empty(capture.GetMessageMetrics());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-METRICS", "concurrent-messages-counted-exactly-once")]
    public async Task Every_sent_message_is_counted_exactly_onceAsync()
    {
        const int count = 200;
        var capture = new MessageMetricCapture(count);
        Guid[] ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();

        await Task.WhenAll(ids.Select(async id =>
        {
            await capture.SentAsync(id, () => capture.PostSendAsync(id), true);
            await ConsumeAsync(capture, id);
        }));

        Assert.True(capture.SendCompleted.IsCompletedSuccessfully);
        Assert.True(capture.ConsumeCompleted.IsCompletedSuccessfully);
        MessageMetric[] metrics = capture.GetMessageMetrics();
        Assert.Equal(count, metrics.Length);
        Assert.Equal(ids.Order(), metrics.Select(metric => metric.MessageId).Order());
    }
}

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
