namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System.Net;
    using NUnit.Framework;


    /// <summary>
    /// The decision the exclusive queue probe makes, held directly.
    /// <para>
    /// The probe waits for a precondition, so the only dangerous mistake it can make is to report
    /// "released" about a queue it knows nothing about: that turns a wait into a success and the spec
    /// behind it into a pass for the wrong reason. Every answer that is not a statement about the queue
    /// therefore has to be unknown, and an unknown never satisfies a wait.
    /// </para>
    /// <para>
    /// These cases need no broker. They exist because the broker cannot be asked to produce a 500 on
    /// demand, and 500 is exactly what the management plugin answers while a queue is being deleted -
    /// the window WaitUntilReleased polls through, and the answer that failed a closing run.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Deciding_what_the_management_api_said
    {
        [Test]
        public void Should_read_an_exclusive_queue_as_held()
        {
            Assert.That(ExclusiveQueueProbe.StateFrom(HttpStatusCode.OK, """{"name":"q","exclusive":true}"""),
                Is.EqualTo(ExclusiveQueueProbe.QueueState.Held));
        }

        [Test]
        public void Should_read_a_queue_that_is_not_exclusive_as_released()
        {
            Assert.That(ExclusiveQueueProbe.StateFrom(HttpStatusCode.OK, """{"name":"q","exclusive":false}"""),
                Is.EqualTo(ExclusiveQueueProbe.QueueState.Released));
        }

        [Test]
        public void Should_read_a_queue_that_is_gone_as_released()
        {
            Assert.That(ExclusiveQueueProbe.StateFrom(HttpStatusCode.NotFound, ""),
                Is.EqualTo(ExclusiveQueueProbe.QueueState.Released));
        }

        [Test]
        public void Should_read_the_deletion_race_as_unknown()
        {
            Assert.That(ExclusiveQueueProbe.StateFrom(HttpStatusCode.InternalServerError, ""),
                Is.EqualTo(ExclusiveQueueProbe.QueueState.Unknown));
        }

        [Test]
        public void Should_read_a_refused_credential_as_unknown()
        {
            Assert.That(ExclusiveQueueProbe.StateFrom(HttpStatusCode.Unauthorized, ""),
                Is.EqualTo(ExclusiveQueueProbe.QueueState.Unknown));
        }

        [Test]
        public void Should_read_an_unparsable_answer_as_unknown()
        {
            Assert.That(ExclusiveQueueProbe.StateFrom(HttpStatusCode.OK, "<html>not json</html>"),
                Is.EqualTo(ExclusiveQueueProbe.QueueState.Unknown));
        }

        /// <summary>
        /// An answer that omits the field is a statement about the queue - the management API reports it
        /// without exclusivity - so it is released rather than unknown. Without this case the two above
        /// would also pass an implementation that answered unknown to everything it did not recognise.
        /// </summary>
        [Test]
        public void Should_read_an_answer_without_the_field_as_released()
        {
            Assert.That(ExclusiveQueueProbe.StateFrom(HttpStatusCode.OK, """{"name":"q"}"""),
                Is.EqualTo(ExclusiveQueueProbe.QueueState.Released));
        }
    }
}
