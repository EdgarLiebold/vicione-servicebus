
// Deliberately a sibling of ViciOne.ServiceBus.RabbitMqTransport.Tests rather than a child of it.
// NUnit applies a SetUpFixture to its own namespace and everything below, so living there made these
// specs need Docker, the management API and a broker to reach their first assertion — a unit test that
// cannot run without the thing it is supposed to be independent of is not a unit test. As a sibling the
// broker set up does not apply, and `dotnet test --filter` on this namespace passes with no fixture
// running and no credentials set. The broker contract lives in BrokerContract_Specs, where it belongs.
namespace ViciOne.ServiceBus.RabbitMqTransport.UnitTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using ViciOne.ServiceBus.Internals;
    using ViciOne.ServiceBus.RabbitMqTransport;


    /// <summary>
    /// Saying a channel or connection is finished and taking it away are two acts, and their order is
    /// the whole point.
    /// <para>
    /// RabbitMQ completes a refused operation's continuation before it raises its shutdown
    /// notification. Disposing from inside that notification therefore pulls the subject out from under
    /// a caller that is still unwinding — and the client's own finally, which releases its RPC
    /// semaphore, then throws ObjectDisposedException over the broker's answer. Measured against the
    /// pinned broker, that replaced four of five real reply code 405 refusals.
    /// </para>
    /// <para>
    /// Every invariant is asserted for both subjects. One type owns both, which is the point: a second
    /// ownership model that differs in its details is a second chance to get the order wrong. Running
    /// the same specs over both is what keeps that claim honest rather than merely stated.
    /// </para>
    /// <para>
    /// Everything that must happen synchronises on TaskCompletionSource, because a race proved by a
    /// delay is not proved. One assertion is the exception, and deliberately so: that disposal has not
    /// begun is a fact about something not happening, and it cannot be read at an instant while the
    /// disposal is queued to the thread pool. That one waits a short bounded time for a signal which
    /// must never arrive — the bound stands for ordering, not for a duration, and it is what makes the
    /// mutation visible at all.
    /// </para>
    /// </summary>
    [TestFixture("channel")]
    [TestFixture("connection")]
    public class Owning_a_transport_subject
    {
        readonly string _subject;

        public Owning_a_transport_subject(string subject)
        {
            _subject = subject;
        }

        [Test]
        public async Task Should_refuse_a_new_operation_the_moment_it_is_invalidated()
        {
            var probe = new DisposalProbe();
            var lifetime = new TransportLifetime(_subject, probe.Dispose);

            Assert.That(lifetime.TryLease(out var first), Is.True, $"an open {_subject} refused an operation");
            first.Dispose();

            lifetime.Invalidate(Refused());

            Assert.That(lifetime.TryLease(out _), Is.False,
                $"a {_subject} the broker has closed still handed out an operation");

            await probe.Started.OrTimeout(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// The defect itself. While an operation holds its lease, disposal must not begin — not even
        /// after the broker has closed the subject and the notification has arrived. For the connection
        /// the operation being protected is above all the creation of a channel.
        /// </summary>
        [Test]
        public async Task Should_not_dispose_while_an_operation_is_still_running()
        {
            var probe = new DisposalProbe();
            var lifetime = new TransportLifetime(_subject, probe.Dispose);

            Assert.That(lifetime.TryLease(out var running), Is.True);

            lifetime.Invalidate(Refused());

            // A bounded wait for a signal that must not arrive, not a sleep standing in for a proof.
            // "Disposal has not begun" cannot be observed at an instant: the disposal is queued to the
            // thread pool, so an immediate check races it and passes whatever the implementation does.
            // Measured: with an await Task.Yield() here, a mutant that disposes regardless of the active
            // lease stayed green. The bound is short because the queued item runs in microseconds; what
            // is asserted is the ordering, not the duration.
            var disposedTooEarly = await Task.WhenAny(probe.Started, Task.Delay(TimeSpan.FromSeconds(1)));

            Assert.That(disposedTooEarly, Is.Not.SameAs(probe.Started),
                $"the {_subject} was disposed while an operation was still holding it, which is exactly "
                + "what replaces the broker's answer with an ObjectDisposedException");

            running.Dispose();

            await probe.Started.OrTimeout(TimeSpan.FromSeconds(10));
        }

        [Test]
        public async Task Should_dispose_exactly_once_however_often_it_is_asked()
        {
            var probe = new DisposalProbe();
            var lifetime = new TransportLifetime(_subject, probe.Dispose);

            lifetime.Invalidate(Refused());
            lifetime.Invalidate(Refused());

            await probe.Started.OrTimeout(TimeSpan.FromSeconds(10));

            await lifetime.DisposeAsync();
            await lifetime.DisposeAsync();

            Assert.That(probe.Count, Is.EqualTo(1), $"the {_subject} was disposed {probe.Count} times");
        }

        /// <summary>
        /// The reason is kept, not summarised. A caller arriving after the close learns what the broker
        /// said, with its initiator, reply code and text intact.
        /// </summary>
        [Test]
        public void Should_give_a_later_caller_the_reason_the_broker_gave()
        {
            var lifetime = new TransportLifetime(_subject, () => Task.CompletedTask);
            var reason = Refused();

            lifetime.Invalidate(reason);

            var refusal = lifetime.NotAvailable();

            Assert.Multiple(() =>
            {
                Assert.That(refusal.ShutdownReason, Is.SameAs(reason), "the reason was rebuilt instead of kept");
                Assert.That(refusal.ShutdownReason.ReplyCode, Is.EqualTo(405));
                Assert.That(refusal.ShutdownReason.Initiator, Is.EqualTo(ShutdownInitiator.Peer));
            });
        }

        /// <summary>
        /// Without a reason nothing is invented. The refusal then says only what this transport can
        /// honestly say, and does not claim the broker answered.
        /// </summary>
        [Test]
        public void Should_not_invent_a_reason_when_there_is_none()
        {
            var lifetime = new TransportLifetime(_subject, () => Task.CompletedTask);

            lifetime.Invalidate(null);

            var refusal = lifetime.NotAvailable();

            Assert.Multiple(() =>
            {
                Assert.That(refusal.ShutdownReason.Initiator, Is.EqualTo(ShutdownInitiator.Library),
                    $"a locally closed {_subject} claimed the peer had answered");
                Assert.That(refusal.ShutdownReason.ReplyCode, Is.EqualTo(491));
                Assert.That(refusal.ShutdownReason.ReplyText, Does.Contain(_subject),
                    "the refusal did not say which subject is gone");
            });
        }

        /// <summary>
        /// The owner's own disposal may wait — unlike the notification, which may not. It returns only
        /// once the subject is really gone.
        /// </summary>
        [Test]
        public async Task Should_wait_for_the_running_operation_before_its_own_disposal_returns()
        {
            var probe = new DisposalProbe();
            var lifetime = new TransportLifetime(_subject, probe.Dispose);

            Assert.That(lifetime.TryLease(out var running), Is.True);

            var disposing = lifetime.DisposeAsync().AsTask();

            await Task.Yield();

            Assert.That(disposing.IsCompleted, Is.False, "disposal returned while an operation was still running");

            running.Dispose();

            await disposing.OrTimeout(TimeSpan.FromSeconds(10));

            Assert.That(probe.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// A lease released twice releases once. A struct will not do: C# copies it silently, and a copy
        /// disposed a second time takes the active count below zero — from where an operation still
        /// running can no longer hold disposal off, which is the defect all of the above exists to
        /// prevent.
        /// </summary>
        [Test]
        public async Task Should_release_a_lease_only_once_however_often_it_is_disposed()
        {
            var probe = new DisposalProbe();
            var lifetime = new TransportLifetime(_subject, probe.Dispose);

            Assert.That(lifetime.TryLease(out var first), Is.True);
            Assert.That(lifetime.TryLease(out var second), Is.True);

            first.Dispose();
            first.Dispose();
            first.Dispose();

            lifetime.Invalidate(Refused());

            await Task.Yield();

            Assert.That(probe.Started.IsCompleted, Is.False,
                "a lease released three times was counted three times, so disposal began while the "
                + "second operation was still running");

            second.Dispose();

            await probe.Started.OrTimeout(TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// A disposal that throws is reported to the owner that waited for it, with its own stack —
        /// not swallowed, and not turned into an unobserved exception surfacing somewhere unrelated.
        /// </summary>
        [Test]
        public async Task Should_report_a_failing_disposal_to_the_owner_that_waits_for_it()
        {
            var failure = new InvalidOperationException("the socket was already gone");
            var lifetime = new TransportLifetime(_subject, () => Task.FromException(failure));

            var thrown = Assert.CatchAsync(async () => await lifetime.DisposeAsync());

            Assert.That(thrown, Is.SameAs(failure), "the disposal failure was replaced or swallowed");

            await lifetime.DisposeAsync().AsTask().ContinueWith(_ => { });
        }

        /// <summary>
        /// The same failure with nobody waiting. Disposal is started by whoever released the last lease,
        /// which is usually a thread pool item no one will ever await, so the failure must not become an
        /// unobserved task exception — and must still be reported to an owner that asks afterwards.
        /// </summary>
        [Test]
        public async Task Should_survive_a_failing_disposal_that_nobody_is_waiting_for()
        {
            var failure = new InvalidOperationException("the socket was already gone");
            var disposed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var lifetime = new TransportLifetime(_subject, () =>
            {
                disposed.TrySetResult(true);
                return Task.FromException(failure);
            });

            lifetime.Invalidate(Refused());

            await disposed.Task.OrTimeout(TimeSpan.FromSeconds(10));

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var thrown = Assert.CatchAsync(async () => await lifetime.DisposeAsync());

            Assert.That(thrown, Is.SameAs(failure),
                "the failure of a disposal nobody waited for was lost instead of kept for whoever asks");
        }

        static ShutdownEventArgs Refused()
        {
            return new ShutdownEventArgs(ShutdownInitiator.Peer, 405,
                "RESOURCE_LOCKED - cannot obtain exclusive access to locked queue 'exclusively-yours'", 50, 10);
        }


        /// <summary>Records when disposal began and how often, which is what the rules above are about.</summary>
        class DisposalProbe
        {
            readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int _count;

            public Task Started => _started.Task;
            public int Count => Volatile.Read(ref _count);

            public Task Dispose()
            {
                Interlocked.Increment(ref _count);

                _started.TrySetResult(true);

                return Task.CompletedTask;
            }
        }
    }
}
