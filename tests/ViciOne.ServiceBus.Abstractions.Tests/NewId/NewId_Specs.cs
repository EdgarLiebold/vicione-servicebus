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

        // A case that generated ten identifiers, compared each with the one before it and printed them
        // stood here. Should_generate_unique_identifiers_with_each_invocation asserts the same property
        // over two hundred thousand identifiers and compares every one with every other, so the weaker
        // one is gone rather than kept beside it.

        [Test]
        public void Should_be_using_the_correct_algorithm()
        {
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
        /// The process id is what separates two identifiers whose every other input is the same.
        /// <para>
        /// Every input the generator reads is fixed here: the same tick, the same worker id, the same
        /// starting sequence. Only the process id provider differs, so nothing else can account for a
        /// difference between the identifiers. The equal pair is the control: without it, an identifier
        /// that differs for any reason at all would satisfy the case.
        /// </para>
        /// </summary>
        [Test]
        public void Should_let_the_process_id_separate_two_otherwise_equal_generators()
        {
            var moment = new DateTime(2026, 8, 17, 21, 4, 5, DateTimeKind.Utc);

            var withoutProcessId = Generator(moment, null).Next();
            var withProcessId = Generator(moment, new FixedProcessIdProvider(0x1A, 0x2B)).Next();
            var withOtherProcessId = Generator(moment, new FixedProcessIdProvider(0x3C, 0x4D)).Next();
            var withSameProcessIdAgain = Generator(moment, new FixedProcessIdProvider(0x1A, 0x2B)).Next();

            Assert.Multiple(() =>
            {
                Assert.That(withProcessId.Timestamp, Is.EqualTo(moment), "the process id provider moved the timestamp");
                Assert.That(withProcessId.ToString(), Is.Not.EqualTo(withoutProcessId.ToString()),
                    "a process id provider left the identifier unchanged");
                Assert.That(withProcessId.ToString(), Is.Not.EqualTo(withOtherProcessId.ToString()),
                    "two different process ids produced the same identifier");
                Assert.That(withProcessId.ToString(), Is.EqualTo(withSameProcessIdAgain.ToString()),
                    "two generators that differ in nothing produced different identifiers, so the case above proves nothing");
            });
        }

        static NewIdGenerator Generator(DateTime moment, IProcessIdProvider processIdProvider)
        {
            return new NewIdGenerator(new FixedTickProvider(moment), new FixedWorkerIdProvider(), processIdProvider);
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


        /// <summary>The same worker id for every generator, so it cannot account for a difference.</summary>
        class FixedWorkerIdProvider :
            IWorkerIdProvider
        {
            public byte[] GetWorkerId(int index)
            {
                return new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
            }
        }


        /// <summary>A process id the test chooses, so it is the only input that varies.</summary>
        class FixedProcessIdProvider :
            IProcessIdProvider
        {
            readonly byte[] _processId;

            public FixedProcessIdProvider(byte first, byte second)
            {
                _processId = new[] { first, second };
            }

            public byte[] GetProcessId()
            {
                return _processId;
            }
        }
    }
}
