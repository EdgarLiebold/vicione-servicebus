namespace ViciOne.ServiceBus.Diagnostics.Tests;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;


/// <summary>
/// Where the exact set is read, and why it may only be read there.
/// <para>
/// The ledger is scanned without a lock, so a read that runs while handlers are still counting belongs
/// to no single moment of the run: the scan passes an identity, a consumer then makes it a duplicate,
/// and the scan walks on and reports neither the duplicate nor a total that was ever true. A lock in
/// the scan would move contention into the path the diagnostic measures. So the order is the contract -
/// observe, come to a standstill, then read - and these cases hold the standstill half of it.
/// </para>
/// </summary>
[TestFixture]
public class Bringing_the_consumer_to_a_standstill_before_the_snapshot
{
    [Test]
    public async Task Should_report_a_standstill_when_the_stop_finished_inside_its_budget()
    {
        var quiesced = await PublishLoadScenario.Quiesce(_ => Task.CompletedTask, TimeSpan.FromSeconds(5));

        Assert.That(quiesced, Is.True);
    }

    [Test]
    public async Task Should_not_report_a_standstill_when_the_stop_swallowed_its_own_cancellation()
    {
        // This is the shape the transport really has. On cancellation the consumer agent logs, cancels
        // the pending consumers and completes the stop regardless, so a stop that returns normally is
        // not by itself proof that it drained. Reading only the exception would have counted this as a
        // standstill and taken the snapshot while a handler could still be running.
        var quiesced = await PublishLoadScenario.Quiesce(
            async token =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }
                catch (OperationCanceledException)
                {
                }
            },
            TimeSpan.FromMilliseconds(200));

        Assert.That(quiesced, Is.False,
            "a stop that ran out of its budget was counted as a standstill, so the snapshot could be "
            + "taken while a handler was still counting");
    }

    [Test]
    public async Task Should_not_report_a_standstill_when_the_stop_raised_its_cancellation()
    {
        var quiesced = await PublishLoadScenario.Quiesce(
            token => Task.Delay(Timeout.InfiniteTimeSpan, token), TimeSpan.FromMilliseconds(200));

        Assert.That(quiesced, Is.False);
    }

    [Test]
    public async Task Should_pass_the_bounded_token_to_the_stop_it_asks_for()
    {
        CancellationToken handed = CancellationToken.None;

        await PublishLoadScenario.Quiesce(token =>
        {
            handed = token;

            return Task.CompletedTask;
        }, TimeSpan.FromSeconds(5));

        Assert.That(handed.CanBeCanceled, Is.True,
            "the stop was asked for without a bound, so a consumer that never finishes would hold the "
            + "whole diagnostic instead of being reported");
    }

    [Test]
    public async Task Should_read_the_ledger_only_after_the_consumer_has_come_to_a_standstill()
    {
        var ledger = new MessageSequenceLedger(2);
        ledger.Observed(0);

        // What a handler does while the stop is draining still belongs to this run. A read taken
        // before the stop misses it, and the set then looks incomplete for a reason that is the
        // reader's own doing rather than the run's.
        (var quiesced, MessageSequenceLedger.Snapshot snapshot) = await PublishLoadScenario.ObserveThenQuiesceThenRead(
            ledger,
            _ =>
            {
                ledger.Observed(1);

                return Task.CompletedTask;
            },
            TimeSpan.Zero, TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.That(quiesced, Is.True);
        Assert.That(snapshot.IsExact, Is.True,
            "the snapshot was taken before the stop had drained, so what the last handler did is "
            + "missing from the set the verdict is read from");
    }

    [Test]
    public async Task Should_observe_for_the_whole_window_before_it_asks_for_the_stop()
    {
        var window = TimeSpan.FromMilliseconds(300);
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        TimeSpan atTheStop = TimeSpan.Zero;

        await PublishLoadScenario.ObserveThenQuiesceThenRead(
            new MessageSequenceLedger(1),
            _ =>
            {
                atTheStop = elapsed.Elapsed;

                return Task.CompletedTask;
            },
            window, TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.That(atTheStop, Is.GreaterThanOrEqualTo(window),
            "the consumer was stopped before the observation window had run, so a duplicate still on "
            + "its way would never have been counted");
    }

    [TestCase(false, false, false, "timeout", TestName = "nothing arrived and nothing stood still")]
    [TestCase(false, true, true, "timeout", TestName = "not everything arrived")]
    [TestCase(true, false, true, "inconclusive", TestName = "looked exact, but was read against live handlers")]
    [TestCase(true, true, false, "invalid", TestName = "everything arrived and the set was not exact")]
    [TestCase(true, true, true, "exact", TestName = "everything arrived, stood still, and was exact")]
    public void Should_name_the_verdict_of_a_run(bool allSeen, bool quiesced, bool exact, string expected)
    {
        Assert.That(PublishLoadScenario.Outcome(allSeen, quiesced, exact), Is.EqualTo(expected),
            "exactness is the strongest claim of this diagnostic and it may not be made from a snapshot "
            + "that was taken while something could still be counting");
    }
}


/// <summary>
/// Where a result leaves the process, on the path that succeeded and on the path that did not.
/// <para>
/// A caller that asked for a file asked for the result of this run, not only for the result of a run
/// that worked. The failure exits used to build a fresh, empty option set of their own, so a --output
/// that had already been parsed was discarded: the run returned 1, wrote its structured failure to
/// stdout, and left the requested file absent.
/// </para>
/// </summary>
[TestFixture]
public class Writing_the_result_where_the_caller_asked_for_it
{
    [Test]
    public async Task Should_write_a_validation_failure_into_the_file_that_was_asked_for()
    {
        using var sink = new TemporaryFile();

        var code = await Program.Main(["publish-load", "--messages", "0", "--output", sink.Path]);

        Assert.That(code, Is.EqualTo(1));
        Assert.That(File.Exists(sink.Path), Is.True,
            "the run failed after --output had already parsed, and the file the caller asked for was "
            + "never written");

        using JsonDocument written = JsonDocument.Parse(await File.ReadAllTextAsync(sink.Path));

        Assert.Multiple(() =>
        {
            Assert.That(written.RootElement.GetProperty("status").GetString(), Is.EqualTo("failed"));
            Assert.That(written.RootElement.GetProperty("error").GetString(), Does.Contain("--messages"));
        });
    }

    [Test]
    public async Task Should_report_an_unknown_scenario_without_a_sink_it_never_read()
    {
        var code = await Program.Main(["not-a-scenario"]);

        Assert.That(code, Is.EqualTo(1),
            "an unknown scenario is a failure of the call, whatever it was asked to write");
    }

    [Test]
    public async Task Should_leave_no_file_behind_when_the_options_could_not_be_read_at_all()
    {
        using var sink = new TemporaryFile();

        var code = await Program.Main(["publish-load", "--output", sink.Path, "--not-an-option", "x"]);

        Assert.That(code, Is.EqualTo(1));
        Assert.That(File.Exists(sink.Path), Is.False,
            "the option set could not be read, so nothing was successfully parsed and there is no sink "
            + "this run may claim was asked for");
    }


    sealed class TemporaryFile :
        IDisposable
    {
        public TemporaryFile()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                $"vicione-diagnostics-{Guid.NewGuid():N}.json");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
    }
}
