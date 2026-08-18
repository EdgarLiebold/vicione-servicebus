#nullable enable
namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework;
    using Util;


    /// <summary>
    /// What TaskUtil.Await promises, and where that promise stops.
    /// <para>
    /// It promises three things: the calling thread blocks until the task finishes, the task's
    /// exception is rethrown unwrapped rather than as an AggregateException, and the caller's
    /// <see cref="SynchronizationContext"/> is neither read, replaced nor posted to.
    /// </para>
    /// <para>
    /// It does not promise deadlock freedom for sync over async, and the last two cases say so by
    /// measuring it. A task whose continuation captured the caller's context needs that context to be
    /// pumped; the caller blocked inside Await cannot pump it, and the wait does not return until
    /// somebody else does. Asserting the opposite would be a claim this API cannot keep.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Awaiting_a_task_synchronously
    {
        [Test]
        public void Should_return_the_result_of_a_completed_task()
        {
            Assert.That(TaskUtil.Await(() => Task.FromResult(42)), Is.EqualTo(42));
        }

        [Test]
        public void Should_wait_for_a_task_that_completes_later()
        {
            var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            var completing = Task.Run(async () =>
            {
                await Task.Yield();
                source.SetResult(7);
            });

            Assert.That(TaskUtil.Await(() => source.Task), Is.EqualTo(7), "the wait returned before the task completed");

            TaskUtil.Await(() => completing);
        }

        [Test]
        public void Should_rethrow_the_exception_unwrapped()
        {
            // An AggregateException here would mean the caller has to unwrap what it never wrapped.
            var thrown = Assert.Throws<IntentionalTestException>(
                () => TaskUtil.Await(() => Task.FromException(new IntentionalTestException("failed on purpose"))));

            Assert.That(thrown.Message, Is.EqualTo("failed on purpose"));
        }

        [Test]
        public void Should_report_cancellation_of_the_token_it_was_given()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            Assert.Throws<OperationCanceledException>(() => TaskUtil.Await(() => pending.Task, cancellation.Token),
                "a cancelled token did not end the wait");
        }

        /// <summary>
        /// Work that never captured the caller's context is completed by whoever runs it, so the wait
        /// returns even though the caller's context is never pumped.
        /// </summary>
        [Test]
        public void Should_return_when_the_awaited_work_did_not_capture_the_caller_context()
        {
            using var context = new PumpedSynchronizationContext();
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = Task.Run(async () =>
                {
                    await Task.Yield();
                    source.SetResult(11);
                });

                Assert.That(TaskUtil.Await(() => source.Task), Is.EqualTo(11),
                    "the wait did not return although nothing it waited for needed the caller's context");

                Assert.That(context.Posted, Is.Zero, "the wait posted a continuation to the caller's context");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        /// <summary>
        /// The limit, measured rather than claimed away. The awaited task really does capture the
        /// caller's context here, so its continuation is posted to a pump that only the blocked caller
        /// owns. The wait cannot return until somebody else drains that pump, and this case proves both
        /// halves: still waiting after the budget, finished once another thread pumps.
        /// </summary>
        [Test]
        public void Should_block_the_caller_when_the_awaited_work_captured_the_caller_context()
        {
            using var context = new PumpedSynchronizationContext();
            var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var returned = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var captured = new TaskCompletionSource<Task<int>>(TaskCreationOptions.RunContinuationsAsynchronously);

            var caller = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(context);

                // Started while the context is current, so its continuation after the await is posted
                // back to that context rather than run on the thread that completes the source.
                async Task<int> ContinuesOnTheCallerContext() => await release.Task + 1;

                Task<int> work = ContinuesOnTheCallerContext();
                captured.SetResult(work);

                returned.SetResult(TaskUtil.Await(() => work));
            }) { IsBackground = true };
            caller.Start();

            TaskUtil.Await(() => captured.Task);
            release.SetResult(41);

            Assert.That(returned.Task.Wait(TimeSpan.FromMilliseconds(500)), Is.False,
                "the wait returned although its continuation was posted to a context nobody pumped");

            context.PumpUntil(returned.Task);

            Assert.That(returned.Task.Wait(TimeSpan.FromSeconds(5)), Is.True, "pumping the context did not release the wait");
            Assert.That(returned.Task.Result, Is.EqualTo(42));
            Assert.That(context.Posted, Is.GreaterThan(0), "no continuation was posted, so this case measured nothing");
        }

        [Test]
        public void Should_leave_the_synchronization_context_of_the_caller_untouched()
        {
            using var context = new PumpedSynchronizationContext();
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                TaskUtil.Await(() => Task.CompletedTask);

                Assert.That(SynchronizationContext.Current, Is.SameAs(context),
                    "the wait replaced the caller's synchronization context and did not put it back");
                Assert.That(context.Posted, Is.Zero, "the wait posted to the caller's context");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        [Test]
        public void Should_carry_no_windows_specific_type_or_reflection()
        {
            // The structural half. On its own it would prove nothing, which is why it stands last.
            var utility = typeof(TaskUtil);

            IEnumerable<Type> nested = utility.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);

            string[] windowsShaped = nested
                .Select(type => type.Name)
                .Where(name => name.Contains("Windows", StringComparison.Ordinal)
                    || name.Contains("Wpf", StringComparison.Ordinal)
                    || name.Contains("Dispatcher", StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(windowsShaped, Is.Empty, "a Windows specific helper returned to the await utility");
        }


        /// <summary>
        /// Queues everything posted to it and runs a callback only when somebody pumps. It counts what
        /// it received, so a case can state that nothing was posted rather than infer it from a value.
        /// </summary>
        sealed class PumpedSynchronizationContext :
            SynchronizationContext,
            IDisposable
        {
            readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
            int _posted;

            /// <summary>How many continuations were handed to this context.</summary>
            public int Posted => Volatile.Read(ref _posted);

            public void Dispose()
            {
                _queue.CompleteAdding();
                _queue.Dispose();
            }

            public override void Post(SendOrPostCallback d, object? state)
            {
                Interlocked.Increment(ref _posted);
                if (!_queue.IsAddingCompleted)
                    _queue.Add((d, state));
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                Interlocked.Increment(ref _posted);
                d(state);
            }

            /// <summary>
            /// Drains the queue from another thread until the given task is done. This is what a caller
            /// blocked inside Await cannot do for itself, which is the entire point of the case that
            /// uses it.
            /// </summary>
            public void PumpUntil(Task completion)
            {
                var pump = new Thread(() =>
                {
                    while (!completion.IsCompleted)
                    {
                        if (_queue.TryTake(out (SendOrPostCallback Callback, object? State) work, TimeSpan.FromMilliseconds(50)))
                            work.Callback(work.State);
                    }
                }) { IsBackground = true };

                pump.Start();
            }
        }
    }
}
