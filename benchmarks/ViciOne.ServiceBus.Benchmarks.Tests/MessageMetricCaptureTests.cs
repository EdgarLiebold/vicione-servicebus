namespace ViciOne.ServiceBus.Benchmarks.Tests;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ViciOneServiceBusBenchmark.BusOutbox;
using ViciOneServiceBusBenchmark.Latency;


/// <summary>
/// The measurement ordering of the send capture, including the postSend mode the bus outbox uses.
/// <para>
/// The ordering matters because the transport's send observer reports completion from inside the send
/// delegate, not after it. Every test that concerns that mode therefore completes from inside the
/// delegate rather than after awaiting it: a test that completes afterwards proves the easy ordering
/// and would have passed against the defect these tests exist for.
/// </para>
/// </summary>
public class MessageMetricCaptureTests
{
    static IReportConsumerMetric Reporting(MessageMetricCapture capture) => capture;

    static Task Consume(MessageMetricCapture capture, Guid messageId) =>
        Reporting(capture).Consumed<object>(messageId);

    /// <summary>The one metric of a capture that saw exactly one message.</summary>
    static MessageMetric SingleMetric(MessageMetricCapture capture) => capture.GetMessageMetrics().Single();

    [Test]
    public async Task The_start_is_registered_before_the_send_delegate_runs()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        // Exactly the real callback order: the observer reports completion while the send is still
        // running. Registering the start after the delegate returned made this throw, and before the
        // registration existed at all it silently produced a zero send timestamp.
        await capture.Sent(messageId, async () =>
        {
            await Task.Yield();
            await capture.PostSend(messageId);
        }, true);
        await Consume(capture, messageId);

        Assert.That(capture.GetMessageMetrics(), Has.Length.EqualTo(1));
    }

    [Test]
    public async Task A_completion_from_inside_the_send_never_measures_from_zero()
    {
        // The defect this file exists for: the completion arrived before the registration, the capture
        // invented an entry whose send timestamp was zero, and the message was then reported as having
        // taken everything since the capture was constructed.
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        Thread.Sleep(20);

        await capture.Sent(messageId, () => capture.PostSend(messageId), true);
        await Consume(capture, messageId);

        var metric = SingleMetric(capture);

        Assert.That(metric.SendCompletionLatency, Is.LessThan(Stopwatch.Frequency / 100),
            "A completion measured from zero would report everything since the capture was created");
        Assert.That(metric.ConsumeLatency, Is.LessThan(Stopwatch.Frequency / 100));
    }

    [Test]
    public void An_unregistered_completion_is_refused_rather_than_invented()
    {
        var capture = new MessageMetricCapture(1);

        Assert.That(async () => await capture.PostSend(Guid.NewGuid()),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task An_unregistered_completion_does_not_advance_the_send_count()
    {
        var capture = new MessageMetricCapture(1);

        Assert.That(async () => await capture.PostSend(Guid.NewGuid()), Throws.TypeOf<InvalidOperationException>());

        Assert.That(capture.SendCompleted.IsCompleted, Is.False,
            "A completion for a message that was never sent must not finish the send series");

        await Task.CompletedTask;
    }

    [Test]
    public async Task A_repeated_completion_counts_once()
    {
        var capture = new MessageMetricCapture(2);
        var first = Guid.NewGuid();

        await capture.Sent(first, async () =>
        {
            await capture.PostSend(first);
            await capture.PostSend(first);
            await capture.PostSend(first);
        }, true);

        Assert.That(capture.SendCompleted.IsCompleted, Is.False,
            "Three completions of one message must not finish a series of two");
    }

    [Test]
    public async Task A_repeated_completion_keeps_the_first_timestamp()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.Sent(messageId, () => capture.PostSend(messageId), true);
        await Consume(capture, messageId);
        var first = SingleMetric(capture).SendCompletionLatency;

        Thread.Sleep(20);
        await capture.PostSend(messageId);

        Assert.That(SingleMetric(capture).SendCompletionLatency, Is.EqualTo(first),
            "A later duplicate must not stretch a latency that was already measured");
    }

    [Test]
    public async Task A_repeated_completion_does_not_add_a_sample()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.Sent(messageId, () => capture.PostSend(messageId), true);
        await capture.PostSend(messageId);
        await Consume(capture, messageId);

        Assert.That(capture.GetMessageMetrics(), Has.Length.EqualTo(1));
    }

    [Test]
    public void A_failed_send_leaves_no_pending_measurement()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        Assert.That(async () => await capture.Sent(messageId, () => throw new InvalidOperationException("send failed")),
            Throws.TypeOf<InvalidOperationException>());

        // If the failed send had left its registration behind, the retry would be refused as a
        // duplicate and the message would be lost to the measurement entirely.
        Assert.That(async () => await capture.Sent(messageId, () => Task.CompletedTask), Throws.Nothing);
    }

    [Test]
    public void A_failed_send_is_not_counted_as_sent()
    {
        var capture = new MessageMetricCapture(1);

        Assert.That(async () => await capture.Sent(Guid.NewGuid(), () => throw new InvalidOperationException("no")),
            Throws.TypeOf<InvalidOperationException>());

        Assert.That(capture.SendCompleted.IsCompleted, Is.False);
    }

    [Test]
    public void Registering_the_same_message_twice_is_refused()
    {
        var capture = new MessageMetricCapture(2);
        var messageId = Guid.NewGuid();

        Assert.That(async () => await capture.Sent(messageId, () => Task.CompletedTask), Throws.Nothing);
        Assert.That(async () => await capture.Sent(messageId, () => Task.CompletedTask),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task The_direct_mode_still_completes_its_own_measurement()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.Sent(messageId, async () =>
        {
            await Task.Yield();
            Thread.Sleep(10);
        });
        await Consume(capture, messageId);

        Assert.That(capture.SendCompleted.IsCompleted, Is.True);
        Assert.That(SingleMetric(capture).SendCompletionLatency, Is.GreaterThan(Stopwatch.Frequency / 200));
    }

    [Test]
    public async Task An_incomplete_send_contributes_no_sample()
    {
        // postSend mode without an observer: the send was registered and run, but nothing ever
        // completed it, so it is not a measurement and must not be reported as one.
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.Sent(messageId, () => Task.CompletedTask, true);
        await Consume(capture, messageId);

        Assert.That(capture.GetMessageMetrics(), Is.Empty);
    }

    [Test]
    public async Task Every_sent_message_is_counted_exactly_once()
    {
        const int count = 200;
        var capture = new MessageMetricCapture(count);
        Guid[] ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();

        await Task.WhenAll(ids.Select(id => Task.Run(async () =>
        {
            await capture.Sent(id, () => capture.PostSend(id), true);
            await Consume(capture, id);
        })));

        Assert.Multiple(() =>
        {
            Assert.That(capture.SendCompleted.IsCompleted, Is.True);
            Assert.That(capture.ConsumeCompleted.IsCompleted, Is.True);
            Assert.That(capture.GetMessageMetrics(), Has.Length.EqualTo(count));
        });
    }
}


/// <summary>
/// The send observer's contract, held where it can be held: the observer must hand the capture's task
/// back rather than swallow it, or a refused completion disappears behind a send that looked fine.
/// </summary>
public class SendMetricReporterTests
{
    [Test]
    public void A_send_without_a_message_id_is_refused()
    {
        Assert.That(() => SendMetricReporter.Report(new MessageMetricCapture(1), null),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void An_unregistered_completion_surfaces_through_the_reporter()
    {
        var capture = new MessageMetricCapture(1);

        Assert.That(async () => await SendMetricReporter.Report(capture, Guid.NewGuid()),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void A_faulted_capture_task_is_not_swallowed()
    {
        // This is the case that actually holds the invariant. The real capture refuses synchronously,
        // so its exception escapes whether the reporter returns the task or drops it — that test
        // passes against a reporter that discards, and cannot hold this. A metric that reports its
        // refusal through the task instead of the stack separates the two: dropping the task here
        // turns the refusal into a silent success.
        Assert.That(async () => await SendMetricReporter.Report(new FaultingMetric(), Guid.NewGuid()),
            Throws.TypeOf<InvalidOperationException>());
    }


    class FaultingMetric :
        IReportConsumerMetric
    {
        public Task Consumed<T>(Guid messageId)
            where T : class => Task.CompletedTask;

        public Task Sent(Guid messageId, Func<Task> send, bool postSend = false) => Task.CompletedTask;

        public Task PostSend(Guid messageId) =>
            Task.FromException(new InvalidOperationException("the capture refused this completion"));
    }

    [Test]
    public async Task A_registered_message_is_completed_through_the_reporter()
    {
        var capture = new MessageMetricCapture(1);
        var messageId = Guid.NewGuid();

        await capture.Sent(messageId, () => SendMetricReporter.Report(capture, messageId), true);

        Assert.That(capture.SendCompleted.IsCompleted, Is.True);
    }
}
