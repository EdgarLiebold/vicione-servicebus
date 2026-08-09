// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-08.
namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// Regression cover for the defect where the test timeout was granted once per fixture instead of
    /// once per test. NUnit builds one fixture instance and runs every test method on it, so a single
    /// budget was shared: whatever the early tests spent was missing from the later ones, and once it
    /// was gone every remaining test that honoured the token failed instantly regardless of its own
    /// speed. Measured before the fix, a test that passes alone in 60.3 s failed after 0.037 s once two
    /// predecessors in the same fixture had spent 40 s.
    /// <para>
    /// The budgets below are deliberately small and the waits are bounded, so these tests are quick and
    /// deterministic rather than sleeping their way to a result.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Granting_the_test_timeout
    {
        class ProbeHarness :
            AsyncTestHarness
        {
        }

        static async Task<bool> BecomesCancelledWithin(CancellationToken token, TimeSpan budget)
        {
            var timer = Stopwatch.StartNew();
            while (timer.Elapsed < budget)
            {
                if (token.IsCancellationRequested)
                    return true;

                await Task.Delay(20).ConfigureAwait(false);
            }

            return token.IsCancellationRequested;
        }

        [Test]
        public void Should_grant_the_whole_budget_again_after_a_predecessor_spent_most_of_it()
        {
            using var harness = new ProbeHarness { TestTimeout = TimeSpan.FromSeconds(2) };

            // The first test opens the budget and burns most of it.
            harness.BeginTestScope();
            var first = harness.TestCancellationToken;
            Assert.That(first.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(1500)), Is.False,
                "the first scope was cancelled before its own budget elapsed");

            // The second test must start from the full timeout, not from what is left of the first.
            harness.BeginTestScope();
            Assert.That(harness.TestCancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(1000)), Is.False,
                "the second scope inherited the remaining time of the first instead of a fresh budget");
        }

        [Test]
        public async Task Should_not_reuse_a_budget_that_already_expired()
        {
            using var harness = new ProbeHarness { TestTimeout = TimeSpan.FromMilliseconds(300) };

            harness.BeginTestScope();
            var expired = harness.TestCancellationToken;
            Assert.That(await BecomesCancelledWithin(expired, TimeSpan.FromSeconds(5)), Is.True,
                "the first scope never expired, so the test proves nothing");

            harness.BeginTestScope();
            Assert.Multiple(() =>
            {
                Assert.That(harness.TestCancellationToken.IsCancellationRequested, Is.False,
                    "the expired token was handed to the next test");
                Assert.That(harness.TestCancellationToken, Is.Not.EqualTo(expired),
                    "the next test received the very same expired source");
                Assert.That(harness.TestCancelledTask.IsCompleted, Is.False,
                    "the next test received an already completed cancellation task");
            });
        }

        [Test]
        public void Should_not_reuse_a_budget_that_was_cancelled_by_hand()
        {
            using var harness = new ProbeHarness { TestTimeout = TimeSpan.FromSeconds(30) };

            harness.BeginTestScope();
            var cancelled = harness.TestCancellationToken;
            harness.Cancel();
            Assert.That(cancelled.IsCancellationRequested, Is.True, "Cancel did not affect the running test");

            harness.BeginTestScope();
            Assert.That(harness.TestCancellationToken.IsCancellationRequested, Is.False,
                "the cancelled state carried over into the next test");
        }

        [Test]
        public async Task Should_cancel_the_awaited_tasks_of_the_current_test_only()
        {
            using var harness = new ProbeHarness { TestTimeout = TimeSpan.FromSeconds(30) };

            harness.BeginTestScope();
            TaskCompletionSource<int> pending = harness.GetTask<int>();
            harness.Cancel();

            Assert.That(await BecomesCancelledWithin(harness.TestCancellationToken, TimeSpan.FromSeconds(2)), Is.True);
            Assert.That(async () => await pending.Task, Throws.InstanceOf<OperationCanceledException>(),
                "Cancel must abort what the current test is awaiting");

            // A task created in the next scope must not be born cancelled.
            harness.BeginTestScope();
            TaskCompletionSource<int> next = harness.GetTask<int>();
            await Task.Delay(100);
            Assert.That(next.Task.IsCompleted, Is.False,
                "the cancellation of the previous test leaked into a task created by the next one");
        }

        [Test]
        public void Should_survive_being_disposed_twice()
        {
            // A container fixture disposes the harness itself and the service provider disposes it
            // again. Cancel on an already disposed source throws, which turned every container fixture
            // teardown into an ObjectDisposedException while the tests themselves stayed green.
            var harness = new ProbeHarness();
            harness.BeginTestScope();
            _ = harness.TestCancellationToken;

            harness.Dispose();
            Assert.That(() => harness.Dispose(), Throws.Nothing);
        }

        [Test]
        public async Task Should_keep_inactivity_working_after_a_predecessor_budget_expired()
        {
            using var harness = new ProbeHarness
            {
                TestTimeout = TimeSpan.FromMilliseconds(300),
                // Deliberately longer than the test budget: the observer's delay must still be pending
                // when the budget expires, otherwise inactivity would have fired first and the test
                // would prove nothing about the binding.
                TestInactivityTimeout = TimeSpan.FromMilliseconds(1200)
            };

            // The bus connects the observer once, while the fixture is being set up.
            harness.BeginTestScope();
            _ = harness.InactivityTask;
            var expired = harness.TestCancellationToken;
            Assert.That(await BecomesCancelledWithin(expired, TimeSpan.FromSeconds(5)), Is.True,
                "the first scope never expired, so the test proves nothing");

            // The observer's timeout loop awaits Task.Delay(timeout, token). Bound to the per test
            // token it died here, silently, and inactivity was dead for every later test.
            harness.BeginTestScope();
            Assert.That(await BecomesCancelledWithin(harness.InactivityToken, TimeSpan.FromSeconds(5)), Is.True,
                "inactivity stopped working once a test budget had expired");
        }
    }


    /// <summary>
    /// The same guarantee through the real NUnit lifecycle: two ordered tests on one fixture instance,
    /// where the first spends most of the budget and the second still receives all of it.
    /// </summary>
    [TestFixture]
    public class Granting_the_test_timeout_through_the_fixture :
        AsyncTestFixture
    {
        class ProbeHarness :
            AsyncTestHarness
        {
        }

        public Granting_the_test_timeout_through_the_fixture()
            : base(new ProbeHarness { TestTimeout = TimeSpan.FromSeconds(2) })
        {
        }

        [Test]
        [Order(1)]
        public void Should_spend_most_of_the_budget()
        {
            Assert.That(TestCancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(1500)), Is.False,
                "the first test was cancelled before its own budget elapsed");
        }

        [Test]
        [Order(2)]
        public void Should_still_receive_the_whole_budget()
        {
            // Without the per test scope this token was already cancelled, and the assertion below
            // failed after a few milliseconds no matter how fast this test is.
            Assert.That(TestCancellationToken.IsCancellationRequested, Is.False,
                "the second test started with the budget its predecessor had spent");
            Assert.That(TestCancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(1000)), Is.False,
                "the second test did not receive the whole configured timeout");
        }
    }
}
