namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Threading.Tasks;
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
        /// A successful answer that does not carry the field is not a released queue. The queue is there
        /// - the API answered about it - and its exclusivity is simply not readable, which is exactly the
        /// shape a schema change would take. Reading it as released is the fail open direction that turns
        /// WaitUntilReleased into a success because the answer was incomplete.
        /// </summary>
        [TestCase("""{"name":"q"}""", TestName = "the field is absent")]
        [TestCase("""{"name":"q","exclusive":null}""", TestName = "the field is null")]
        [TestCase("""{"name":"q","exclusive":"false"}""", TestName = "the field is a string")]
        [TestCase("""{"name":"q","exclusive":0}""", TestName = "the field is a number")]
        [TestCase("""[{"name":"q","exclusive":false}]""", TestName = "the answer is not an object")]
        public void Should_read_an_answer_it_cannot_type_as_unknown(string body)
        {
            Assert.That(ExclusiveQueueProbe.StateFrom(HttpStatusCode.OK, body),
                Is.EqualTo(ExclusiveQueueProbe.QueueState.Unknown));
        }
    }


    /// <summary>
    /// The budget of a wait, held without a broker.
    /// <para>
    /// Two things have to be true and neither is visible from a passing integration run: the wait ends
    /// when its own budget is spent rather than after however many questions it happened to ask, and a
    /// single question is never given more time than the wait has left. A ten second call started one
    /// second before the end of a thirty second budget would otherwise still be running nine seconds
    /// after the wait was over.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Spending_the_budget_of_a_wait
    {
        [Test]
        public async Task Should_stop_when_the_budget_is_spent_although_nothing_is_ever_known()
        {
            var asked = 0;
            var elapsed = Stopwatch.StartNew();

            var last = await ExclusiveQueueProbe.PollUntil(_ =>
                {
                    asked++;
                    return Task.FromResult(ExclusiveQueueProbe.QueueState.Unknown);
                },
                ExclusiveQueueProbe.QueueState.Released, TimeSpan.FromMilliseconds(400),
                TimeSpan.FromMilliseconds(50));

            elapsed.Stop();

            Assert.Multiple(() =>
            {
                Assert.That(last, Is.EqualTo(ExclusiveQueueProbe.QueueState.Unknown),
                    "an unknown answer must never be reported as the wanted state");
                Assert.That(asked, Is.GreaterThan(1), "the wait asked once and gave up, so it did not poll");
                Assert.That(elapsed.Elapsed, Is.LessThan(TimeSpan.FromSeconds(3)),
                    "the wait outlived its budget by a wide margin");
            });
        }

        /// <summary>
        /// A question that takes every second it was granted still ends inside the wait it belongs to.
        /// <para>
        /// This is the property that matters, and it is measured rather than inspected: the reader
        /// consumes exactly the time it was handed, so if it were handed a fixed call limit instead of
        /// what is left, the wait would run for that limit rather than for its budget. Comparing the
        /// handed value against a clock read afterwards cannot state this - the two are read at
        /// different instants and the difference is the measurement, not the defect.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_never_grant_a_question_more_time_than_the_wait_has_left()
        {
            var budget = TimeSpan.FromMilliseconds(400);
            var granted = new List<TimeSpan>();
            var elapsed = Stopwatch.StartNew();

            var last = await ExclusiveQueueProbe.PollUntil(async remaining =>
                {
                    granted.Add(remaining);
                    await Task.Delay(remaining);
                    return ExclusiveQueueProbe.QueueState.Unknown;
                },
                ExclusiveQueueProbe.QueueState.Released, budget, TimeSpan.FromMilliseconds(50));

            elapsed.Stop();

            Assert.Multiple(() =>
            {
                Assert.That(granted, Is.Not.Empty, "the wait asked nothing at all");
                Assert.That(granted, Has.All.LessThanOrEqualTo(budget),
                    "a question was granted more time than the whole wait has");
                Assert.That(granted, Has.All.GreaterThan(TimeSpan.Zero), "a question was asked with no time left");
                Assert.That(elapsed.Elapsed, Is.LessThan(budget + TimeSpan.FromSeconds(1)),
                    "a question that used all of its granted time outlived the wait it belongs to");
                Assert.That(last, Is.EqualTo(ExclusiveQueueProbe.QueueState.Unknown));
            });
        }

        [Test]
        public async Task Should_return_as_soon_as_the_wanted_state_is_reported()
        {
            var answers = new Queue<ExclusiveQueueProbe.QueueState>(new[]
            {
                ExclusiveQueueProbe.QueueState.Unknown,
                ExclusiveQueueProbe.QueueState.Held,
                ExclusiveQueueProbe.QueueState.Released
            });

            var last = await ExclusiveQueueProbe.PollUntil(_ => Task.FromResult(answers.Dequeue()),
                ExclusiveQueueProbe.QueueState.Released, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(10));

            Assert.Multiple(() =>
            {
                Assert.That(last, Is.EqualTo(ExclusiveQueueProbe.QueueState.Released));
                Assert.That(answers, Is.Empty, "the wait did not consume every answer before the wanted one");
            });
        }
    }
}
