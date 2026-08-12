// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-11.
// A sibling of the broker tests' namespace on purpose: NUnit applies the assembly's SetUpFixture to
// its own namespace and below, and classifying an exception needs no broker, no Docker and no
// management API. Living below it meant these specs could not reach an assertion without the very
// infrastructure they are independent of.
namespace ViciOne.ServiceBus.RabbitMqTransport.UnitTests
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using NUnit.Framework;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using ViciOne.ServiceBus.RabbitMqTransport;


    /// <summary>
    /// Which broker answers are worth repeating, and which are not.
    /// <para>
    /// The transport recovers from almost everything, and that is the point: a dropped connection, an
    /// unreachable broker and a closed channel are all situations where trying again is the correct
    /// answer. Two are not. A refused credential will be refused again, and a queue another connection
    /// holds exclusively will not be handed over by asking a second time. Those two end the attempt.
    /// </para>
    /// <para>
    /// The line is drawn per reply code and not over a range. Record 0039 binds that explicitly, and
    /// the 405/406 pair below is the reason: they are neighbours in the numbering and opposites in
    /// meaning. PRECONDITION_FAILED is what the delivery acknowledgement timeout produces, and the
    /// redelivery proved in the same round depends on the transport reconnecting after it.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Classifying_a_broker_error
    {
        [Test]
        public void Should_not_retry_a_queue_held_exclusively_elsewhere()
        {
            var exception = new RabbitMqConnectionException("Declare failed", ChannelClosed(405,
                "RESOURCE_LOCKED - cannot obtain exclusive access to locked queue 'exclusively-yours' in vhost 'test'"));

            Assert.That(exception.IsTransient, Is.False,
                "an exclusively held queue was classified as transient, so the endpoint start disappears into a retry loop again");
        }

        /// <summary>
        /// The neighbour that must keep its retry. If this ever turns non-transient, the redelivery case
        /// stops working: the broker closes the channel on an acknowledgement timeout and the transport
        /// has to come back.
        /// </summary>
        [Test]
        public void Should_retry_a_precondition_failure()
        {
            var exception = new RabbitMqConnectionException("Channel closed", ChannelClosed(406,
                "PRECONDITION_FAILED - delivery acknowledgement on channel 1 timed out. Timeout value used: 60000 ms"));

            Assert.That(exception.IsTransient, Is.True,
                "an acknowledgement timeout was classified as permanent, which stops the redelivery the transport depends on");
        }

        [Test]
        public void Should_retry_an_unreachable_broker()
        {
            var exception = new RabbitMqConnectionException("Broker unreachable",
                new BrokerUnreachableException(new SocketException(10061)));

            Assert.That(exception.IsTransient, Is.True, "an unreachable broker is the case retrying exists for");
        }

        [Test]
        public void Should_not_retry_a_refused_credential()
        {
            var exception = new RabbitMqConnectionException("Broker unreachable",
                new BrokerUnreachableException(new AuthenticationFailureException("ACCESS_REFUSED")));

            Assert.That(exception.IsTransient, Is.False, "a refused credential is not repaired by asking again");
        }

        [Test]
        public void Should_retry_a_dropped_connection()
        {
            var exception = new RabbitMqConnectionException("Operation interrupted", new EndOfStreamException());

            Assert.That(exception.IsTransient, Is.True, "a dropped connection has to stay recoverable");
        }

        /// <summary>
        /// The conflict is recognised wherever it sits in the chain. The transport wraps what it catches
        /// more than once on the way out, and a classification that only looked at the outermost
        /// exception would answer correctly only by accident.
        /// </summary>
        [Test]
        public void Should_recognise_the_conflict_through_a_wrapped_cause()
        {
            var exception = new RabbitMqConnectionException("Declare failed",
                new InvalidOperationException("the pipe faulted", ChannelClosed(405,
                    "RESOURCE_LOCKED - cannot obtain exclusive access to locked queue 'exclusively-yours'")));

            Assert.That(exception.IsTransient, Is.False, "the conflict went unrecognised because it was not the outermost exception");
        }

        /// <summary>
        /// The same refusal, in the shape it takes when the channel is already gone by the time the next
        /// operation runs. AlreadyClosedException derives from OperationInterruptedException, so a rule
        /// written for the base type alone does not cover it — and under load that is the shape the
        /// refusal actually arrives in. The isolated exclusivity spec never produced it; the full suite
        /// did, four declare attempts at a time.
        /// </summary>
        [Test]
        public void Should_not_retry_the_conflict_when_the_channel_is_already_closed()
        {
            var exception = new RabbitMqConnectionException("Channel already closed",
                new AlreadyClosedException(new ShutdownEventArgs(ShutdownInitiator.Peer, 405,
                    "RESOURCE_LOCKED - cannot obtain exclusive access to locked queue 'exclusively-yours'")));

            Assert.That(exception.IsTransient, Is.False,
                "the conflict was classified as transient in its already-closed form, so the declare loop keeps running");
        }

        /// <summary>
        /// A stop is not a refusal, and now says so — without changing what the public constructor says.
        /// <para>
        /// The transport announces its own shutdown through a non-public path that sets the flag to
        /// transient, because a stop is exactly the failure a later start resolves. It used to announce
        /// it with the public string constructor, whose flag is false, so a routine shutdown carried the
        /// same answer as a refused credential and every caller asking "can waiting fix this?" was told
        /// no about a bus that was merely stopping. Measured, that took out unrelated specs across the
        /// suite: always the second or third instance of a fixture, the one that ran after a stop.
        /// </para>
        /// <para>
        /// Both halves are asserted together on purpose. Correcting the internal case by changing the
        /// public constructor would have handed every external caller a new meaning for an unchanged
        /// signature — a behaviour change no signature gate can see. This spec fails if either half
        /// moves.
        /// </para>
        /// </summary>
        [Test]
        public void Should_report_a_stop_as_transient_without_changing_the_public_constructor()
        {
            var stopping = RabbitMqConnectionException.Stopping("rabbitmq://localhost/test");
            var publicMessage = new RabbitMqConnectionException("The connection is stopping and cannot be used: rabbitmq://localhost/test");

            Assert.Multiple(() =>
            {
                Assert.That(stopping.IsTransient, Is.True,
                    "a bus that is merely stopping was reported as a failure waiting cannot fix, which is what "
                    + "made every routine stop look like a dead endpoint");
                Assert.That(publicMessage.IsTransient, Is.False,
                    "the public string constructor changed its meaning, so every external caller silently got a "
                    + "different answer for an unchanged signature");
            });
        }

        /// <summary>
        /// The other half of the same contract: the two answers that stay permanent must not have been
        /// loosened by the correction above. Without this, making the stop transient could quietly make
        /// everything transient and nothing would notice.
        /// </summary>
        [Test]
        public void Should_keep_the_two_permanent_answers_permanent()
        {
            var conflict = new RabbitMqConnectionException("Declare failed", ChannelClosed(405,
                "RESOURCE_LOCKED - cannot obtain exclusive access to locked queue 'exclusively-yours'"));

            var refusedCredential = new RabbitMqConnectionException("Broker unreachable",
                new BrokerUnreachableException(new AuthenticationFailureException("ACCESS_REFUSED")));

            Assert.Multiple(() =>
            {
                Assert.That(conflict.IsTransient, Is.False, "the exclusivity conflict became retryable again");
                Assert.That(refusedCredential.IsTransient, Is.False, "a refused credential became retryable");
            });
        }

        static OperationInterruptedException ChannelClosed(ushort replyCode, string replyText)
        {
            return new OperationInterruptedException(new ShutdownEventArgs(ShutdownInitiator.Peer, replyCode, replyText));
        }
    }
}
