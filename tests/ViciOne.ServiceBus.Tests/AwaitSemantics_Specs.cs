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
    /// What TaskUtil.Await promises on the one platform this product runs on.
    /// <para>
    /// It used to reach Windows Forms and WPF dispatchers through reflection and to install an STA
    /// specific single threaded context in front of the wait. None of that could run here, so every
    /// call already took the path these cases assert. They exist so the removal is a measured
    /// statement about behaviour rather than a text scan over deleted lines.
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

        [Test]
        public void Should_not_deadlock_under_a_synchronization_context_that_serialises_callbacks()
        {
            // The case the removed dispatchers existed for, expressed with a context that works on this
            // platform: a single pump that would deadlock if the wait needed the pump to make progress.
            var context = new PumpedSynchronizationContext();
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
                    "the wait needed the current context to be pumped, which no caller here does");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
                context.Dispose();
            }
        }

        [Test]
        public void Should_leave_the_synchronization_context_of_the_caller_untouched()
        {
            var context = new PumpedSynchronizationContext();
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                TaskUtil.Await(() => Task.CompletedTask);

                Assert.That(SynchronizationContext.Current, Is.SameAs(context),
                    "the wait replaced the caller's synchronization context and did not put it back");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
                context.Dispose();
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
        /// Runs everything posted to it on one pump thread, and only while that thread pumps. A wait
        /// that needed this context to run its continuation would never return.
        /// </summary>
        sealed class PumpedSynchronizationContext :
            SynchronizationContext,
            IDisposable
        {
            readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();

            public void Dispose()
            {
                _queue.CompleteAdding();
                _queue.Dispose();
            }

            public override void Post(SendOrPostCallback d, object? state)
            {
                if (!_queue.IsAddingCompleted)
                    _queue.Add((d, state));
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                d(state);
            }
        }
    }
}
