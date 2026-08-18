namespace ViciOne.ServiceBus.Abstractions.Tests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NewIdProviders;
    using NUnit.Framework;


    [TestFixture]
    public class Using_the_newid_generator
    {
        [Test]
        public void Should_be_able_to_determine_equal_ids()
        {
            var id1 = new NewId("fc070000-9565-3668-e000-08d5893343c6");
            var id2 = new NewId("fc070000-9565-3668-e000-08d5893343c6");

            Assert.That(id1, Is.EqualTo(id2));
        }

        [Test]
        public void Should_be_able_to_determine_greater_id()
        {
            var lowerId = new NewId("fc070000-9565-3668-e000-08d5893343c6");
            var greaterId = new NewId("fc070000-9565-3668-9180-08d589338b38");

            Assert.That(lowerId, Is.LessThan(greaterId));
        }

        [Test]
        public void Should_be_able_to_determine_lower_id()
        {
            var lowerId = new NewId("fc070000-9565-3668-e000-08d5893343c6");
            var greaterId = new NewId("fc070000-9565-3668-9180-08d589338b38");

            Assert.That(lowerId, Is.LessThanOrEqualTo(greaterId));
        }

        [Test]
        public void Should_generate_sequential_ids_quickly()
        {
            NewId.SetTickProvider(new StopwatchTickProvider());
            NewId.Next();

            var limit = 10;

            var ids = new NewId[limit];
            for (var i = 0; i < limit; i++)
                ids[i] = NewId.Next();

            for (var i = 0; i < limit - 1; i++)
            {
                Assert.That(ids[i + 1], Is.Not.EqualTo(ids[i]));
                Console.WriteLine(ids[i]);
            }
        }

        [Test]
        public void Should_be_using_the_correct_algorithm()
        {
            NewId.SetTickProvider(new StopwatchTickProvider());

            var first = NewId.NextGuid();
            Guid[] next = NewId.NextGuid(3);

            for (var i = 0; i < next.Length - 1; i++)
            {
                Assert.That(next[i].ToString().Substring(0, 4), Is.EqualTo(first.ToString().Substring(0, 4)));
                Assert.That(next[i].ToString().Substring(6), Is.EqualTo(next[i + 1].ToString().Substring(6)));
                Assert.That(int.Parse(next[i].ToString().Substring(4, 2)), Is.EqualTo(i));
            }
        }

        [Test]
        public void Should_be_using_the_correct_algorithm_for_sequential_guids()
        {
            NewId.SetTickProvider(new StopwatchTickProvider());

            var first = NewId.NextSequentialGuid();
            var next = new Guid[3];
            NewId.NextSequentialGuid(next, 0, 3);

            for (var i = 0; i < next.Length - 1; i++)
            {
                Assert.That(next[i].ToString().Substring(0,14), Is.EqualTo(first.ToString().Substring(0, 14)));
                Assert.That(next[i].ToString().Substring(19,13), Is.EqualTo(first.ToString().Substring(19, 13)));
                Assert.That(next[i].ToString().Substring(0,32), Is.EqualTo(next[i+1].ToString().Substring(0,32)));
                Assert.That(int.Parse(next[i].ToString().Substring(32,2)), Is.EqualTo(i));
            }
        }

        /// <summary>
        /// The timestamp an identifier carries is the tick its generator was given.
        /// <para>
        /// The imported case read the wall clock, generated an identifier and accepted any timestamp
        /// within a minute of it while printing both for a human. A minute of slack asserts almost
        /// nothing, and it is why the case never ran in a required category.
        /// </para>
        /// <para>
        /// The generator is built here rather than configured on the static NewId. Setting the
        /// process wide tick provider needs the shared generator reset for it to take effect, and
        /// that reset moves the sequence every other test in this assembly shares: it turned
        /// Should_be_using_the_correct_algorithm_for_sequential_guids red on the tick boundary. A
        /// local generator asserts the same thing and disturbs nothing.
        /// </para>
        /// </summary>
        [Test]
        public void Should_carry_the_timestamp_of_the_tick_it_was_given()
        {
            var moment = new DateTime(2026, 8, 17, 21, 4, 5, DateTimeKind.Utc);

            var generator = new NewIdGenerator(new FixedTickProvider(moment), new BestPossibleWorkerIdProvider());

            Assert.That(generator.Next().Timestamp, Is.EqualTo(moment));
        }

        /// <summary>
        /// A process id provider changes the identifier without disturbing its timestamp.
        /// </summary>
        [Test]
        public void Should_carry_the_timestamp_with_a_process_id_provider()
        {
            var moment = new DateTime(2026, 8, 17, 21, 4, 5, DateTimeKind.Utc);

            var plain = new NewIdGenerator(new FixedTickProvider(moment), new BestPossibleWorkerIdProvider());
            var withProcessId = new NewIdGenerator(new FixedTickProvider(moment), new BestPossibleWorkerIdProvider(),
                new CurrentProcessIdProvider());

            var id = withProcessId.Next();

            Assert.Multiple(() =>
            {
                Assert.That(id.Timestamp, Is.EqualTo(moment));
                // Both generators sit on the same fixed tick and start their sequence at the same
                // place, so the process id is the only thing that can separate them. Comparing the
                // whole identifier avoids guessing which part of it carries that value.
                Assert.That(id.ToString(), Is.Not.EqualTo(plain.Next().ToString()),
                    "the process id provider left the identifier unchanged");
            });
        }

        /// <summary>
        /// Generating in parallel produces no repeated identifier.
        /// <para>
        /// The imported case generated twenty million identifiers across eight degrees of parallelism
        /// and then printed how many duplicates it had found, asserting nothing at all. The count here
        /// is bounded so the case can live in a required category, and the outcome is a sentence
        /// rather than console output.
        /// </para>
        /// </summary>
        [Test]
        public void Should_be_thread_safe_and_produce_no_duplicate()
        {
            const int Count = 200_000;

            var ids = new NewId[Count];

            Parallel.For(0, Count, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i => ids[i] = NewId.Next());

            Assert.That(ids.Distinct().Count(), Is.EqualTo(Count), "a parallel run repeated an identifier");
        }

        /// <summary>
        /// Generating in sequence produces no repeated identifier.
        /// <para>
        /// The imported case compared each identifier only with the one before it, which a generator
        /// that cycles through a small set would pass. Every identifier is compared with every other
        /// one here, by counting the distinct ones.
        /// </para>
        /// </summary>
        [Test]
        public void Should_generate_unique_identifiers_with_each_invocation()
        {
            const int Count = 200_000;

            var ids = new NewId[Count];
            for (var i = 0; i < Count; i++)
                ids[i] = NewId.Next();

            Assert.That(ids.Distinct().Count(), Is.EqualTo(Count), "a sequential run repeated an identifier");
        }


        /// <summary>A tick source the test chooses, so a timestamp can be compared exactly.</summary>
        class FixedTickProvider :
            ITickProvider
        {
            readonly long _ticks;

            public FixedTickProvider(DateTime moment)
            {
                _ticks = moment.Ticks;
            }

            public long Ticks => _ticks;
        }

    }
}
