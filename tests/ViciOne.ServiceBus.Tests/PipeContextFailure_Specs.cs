// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-11.
namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Agents;
    using ViciOne.ServiceBus.Middleware;


    /// <summary>
    /// A failure while tidying up never replaces the failure that caused it.
    /// <para>
    /// PipeContextSupervisor.Send reported the fault and then rethrew. Reporting a fault is itself
    /// clean up, and clean up can fail: when it did, the rethrow was never reached and the caller
    /// received the failure of the clean up instead of the one that mattered. The same held for the
    /// finally block, which replaces whatever is propagating if it throws.
    /// </para>
    /// <para>
    /// Measured on the real transport, that turned a broker refusing an exclusively held queue —
    /// reply code 405, a permanent answer — into "channel is already closed", a reply code this
    /// transport invents for a channel it has itself discarded. The refusal was then classified as
    /// worth retrying, and a caller waiting for the endpoint was left to the sixty second readiness
    /// limit with nothing naming the cause. The rule is asserted here rather than at the transport
    /// because it is not about RabbitMQ: whatever the operation was, its exception is the answer.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Failing_while_cleaning_up_after_a_failure
    {
        [Test]
        public void Should_report_the_operation_failure_and_not_the_reporting_failure()
        {
            var supervisor = new PipeContextSupervisor<TestContext>(new Factory(faultThrows: true));

            var thrown = Assert.ThrowsAsync<OperationFailed>(
                async () => await supervisor.Send(new Failing(), CancellationToken.None),
                "the caller received the failure of the fault reporting instead of the operation's own");

            Assert.That(thrown.Message, Is.EqualTo("the operation failed"));
        }

        [Test]
        public void Should_report_the_operation_failure_and_not_a_failing_stop()
        {
            var supervisor = new PipeContextSupervisor<TestContext>(new Factory(stopThrows: true));

            Assert.ThrowsAsync<OperationFailed>(
                async () => await supervisor.Send(new Failing(), CancellationToken.None),
                "a stop that faulted replaced the operation's own failure on the way out");
        }

        [Test]
        public void Should_report_the_operation_failure_and_not_a_failing_dispose()
        {
            var supervisor = new PipeContextSupervisor<TestContext>(new Factory(disposeThrows: true));

            Assert.ThrowsAsync<OperationFailed>(
                async () => await supervisor.Send(new Failing(), CancellationToken.None),
                "a dispose that faulted replaced the operation's own failure on the way out");
        }

        /// <summary>
        /// All three at once, which is what the transport actually did: the broker closes the channel,
        /// so reporting, stopping and disposing it all fail for the same reason.
        /// </summary>
        [Test]
        public void Should_report_the_operation_failure_when_everything_else_fails_too()
        {
            var supervisor = new PipeContextSupervisor<TestContext>(new Factory(true, true, true));

            var thrown = Assert.ThrowsAsync<OperationFailed>(
                async () => await supervisor.Send(new Failing(), CancellationToken.None));

            Assert.That(thrown.Message, Is.EqualTo("the operation failed"));
        }

        /// <summary>
        /// The idempotency boundary, and it is a deliberate change of behaviour rather than a
        /// preservation of it.
        /// <para>
        /// Before, a stop or dispose that failed after the operation had already succeeded was thrown
        /// at the caller. The caller cannot tell that failure apart from one of the operation itself,
        /// so it retries — and the send it retries has already been delivered. A clean up failure must
        /// therefore not turn a completed operation into a reported one, or the price of tidying up is
        /// a duplicate message.
        /// </para>
        /// <para>
        /// Named here rather than described as "unchanged", because it is not: what changes is that a
        /// failure after success stays diagnostic instead of becoming the caller's answer.
        /// </para>
        /// </summary>
        [Test]
        public void Should_not_turn_a_delivered_operation_into_a_failed_one()
        {
            var supervisor = new PipeContextSupervisor<TestContext>(new Factory(true, true, true));
            var pipe = new Counting();

            Assert.DoesNotThrowAsync(async () => await supervisor.Send(pipe, CancellationToken.None),
                "a clean up failure after a delivered operation was reported to the caller, who cannot tell it from a "
                + "failure of the operation and would retry a message that has already been sent");

            Assert.That(pipe.Sent, Is.EqualTo(1), "the operation ran more than once");
        }


        public class OperationFailed :
            Exception
        {
            public OperationFailed(string message)
                : base(message)
            {
            }
        }


        class CleanupFailed :
            Exception
        {
            public CleanupFailed(string message)
                : base(message)
            {
            }
        }


        public class TestContext :
            BasePipeContext
        {
        }


        class Failing :
            IPipe<TestContext>
        {
            public Task Send(TestContext context)
            {
                throw new OperationFailed("the operation failed");
            }

            public void Probe(ProbeContext context)
            {
            }
        }


        class Counting :
            IPipe<TestContext>
        {
            public int Sent { get; private set; }

            public Task Send(TestContext context)
            {
                Sent++;

                return Task.CompletedTask;
            }

            public void Probe(ProbeContext context)
            {
            }
        }


        class Factory :
            IPipeContextFactory<TestContext>
        {
            readonly bool _disposeThrows;
            readonly bool _faultThrows;
            readonly bool _stopThrows;

            public Factory(bool faultThrows = false, bool stopThrows = false, bool disposeThrows = false)
            {
                _faultThrows = faultThrows;
                _stopThrows = stopThrows;
                _disposeThrows = disposeThrows;
            }

            public IPipeContextAgent<TestContext> CreateContext(ISupervisor supervisor)
            {
                IAsyncPipeContextAgent<TestContext> agent = supervisor.AddAsyncContext<TestContext>();

                agent.Created(new TestContext());

                return agent;
            }

            public IActivePipeContextAgent<TestContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<TestContext> context,
                CancellationToken cancellationToken)
            {
                return new BreakingActiveContext(context, _faultThrows, _stopThrows, _disposeThrows);
            }
        }


        /// <summary>An active context whose clean up fails, in each of the three ways it can.</summary>
        class BreakingActiveContext :
            IActivePipeContextAgent<TestContext>
        {
            readonly PipeContextHandle<TestContext> _context;
            readonly bool _disposeThrows;
            readonly bool _faultThrows;
            readonly bool _stopThrows;

            public BreakingActiveContext(PipeContextHandle<TestContext> context, bool faultThrows, bool stopThrows, bool disposeThrows)
            {
                _context = context;
                _faultThrows = faultThrows;
                _stopThrows = stopThrows;
                _disposeThrows = disposeThrows;
            }

            public Task<TestContext> Context => _context.Context;

            public bool IsDisposed => false;

            public CancellationToken Stopping => CancellationToken.None;
            public CancellationToken Stopped => CancellationToken.None;
            public Task Ready => Task.CompletedTask;
            public Task Completed => Task.CompletedTask;

            public Task Faulted(Exception exception)
            {
                if (_faultThrows)
                    throw new CleanupFailed("reporting the fault failed");

                return Task.CompletedTask;
            }

            public Task Stop(StopContext context)
            {
                if (_stopThrows)
                    throw new CleanupFailed("stopping failed");

                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync()
            {
                if (_disposeThrows)
                    throw new CleanupFailed("disposing failed");

                return default;
            }
        }
    }
}
