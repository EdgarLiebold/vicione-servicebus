namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Internals;
    using ViciOne.ServiceBus.Testing;
    using NUnit.Framework;
    using TestFramework;
    using TestFramework.Messages;


    [TestFixture]
    public class Receiving_a_single_message_in_a_batch_and_it_faults :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_move_the_message_to_the_error_queue()
        {
            await InputQueueSendEndpoint.Send(new PingMessage());

            ConsumeContext<PingMessage> batch = await _errorHandler;
        }

        public Receiving_a_single_message_in_a_batch_and_it_faults()
        {
            InMemoryTestHarness.TestTimeout = TimeSpan.FromSeconds(5);
        }

        FailingBatchConsumer _consumer;
        Task<ConsumeContext<PingMessage>> _errorHandler;

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint("input_queue_error", x =>
            {
                _errorHandler = Handled<PingMessage>(x);
            });
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _consumer = new FailingBatchConsumer();

            configurator.Batch<PingMessage>(x =>
            {
                x.MessageLimit = 2;
                x.TimeLimit = TimeSpan.FromMilliseconds(500);

                x.Consumer(() => _consumer);
            });
        }
    }


    [TestFixture]
    public class Receiving_a_single_message_in_a_single_message_batch_and_it_faults :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_move_the_message_to_the_error_queue()
        {
            await InputQueueSendEndpoint.Send(new PingMessage());

            ConsumeContext<PingMessage> batch = await _errorHandler;
        }

        FailingBatchConsumer _consumer;
        Task<ConsumeContext<PingMessage>> _errorHandler;

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ReceiveEndpoint("input_queue_error", x =>
            {
                _errorHandler = Handled<PingMessage>(x);
            });
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _consumer = new FailingBatchConsumer();

            configurator.Batch<PingMessage>(x =>
            {
                x.MessageLimit = 1;
                x.TimeLimit = TimeSpan.FromMilliseconds(500);

                x.Consumer(() => _consumer);
            });
        }
    }


    [TestFixture]
    public class Receiving_and_grouping_messages :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_receive_one_batch_per_group()
        {
            var correlation1 = NewId.NextGuid();
            var correlation2 = NewId.NextGuid();

            await InputQueueSendEndpoint.Send(new PingMessage());
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation1));
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation1));
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation2));
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation2));
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation2));

            var count = await BusTestHarness.Consumed.SelectAsync<PingMessage>().Take(6).Count();

            Batch<PingMessage>[] batches = await CollectedBatches();

            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(6));

                Assert.That(batches.Select(x => x.Length), Is.EquivalentTo(new[] { 1, 2, 3 }));
            });
        }

        /// <summary>How many groups the six messages above fall into: one, two and three.</summary>
        const int ExpectedBatches = 3;

        readonly List<Task<Batch<PingMessage>>> _batches = new List<Task<Batch<PingMessage>>>();
        readonly TaskCompletionSource<bool> _allStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// The batches that were delivered, awaited rather than sampled.
        /// <para>
        /// Counting the six consumed messages says nothing about the batches: the last group's consumer
        /// can still be running when the sixth message is counted, and reading the list at that moment
        /// found two of three. The run then reported "observed &lt; 3, 1 &gt;, missing &lt; 2 &gt;" for a
        /// delivery that had in fact happened — the failure this fixture was labelled flaky for. Waiting
        /// for all three consumers to exist and then awaiting their batches removes the race without
        /// touching the assertion. No delay and no repetition: a race a sleep hides is still there.
        /// </para>
        /// </summary>
        async Task<Batch<PingMessage>[]> CollectedBatches()
        {
            await _allStarted.Task.OrTimeout(TimeSpan.FromSeconds(30));

            Task<Batch<PingMessage>>[] delivered;
            lock (_batches)
                delivered = _batches.ToArray();

            return await Task.WhenAll(delivered).OrTimeout(TimeSpan.FromSeconds(30));
        }

        /// <summary>Registers a batch consumer's delivery, and reports when the last group has one.</summary>
        void Track(Task<Batch<PingMessage>> batch)
        {
            lock (_batches)
            {
                _batches.Add(batch);

                if (_batches.Count == ExpectedBatches)
                    _allStarted.TrySetResult(true);
            }
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            configurator.ConcurrentMessageLimit = 10;

            configurator.Consumer(() =>
            {
                TaskCompletionSource<Batch<PingMessage>> tcs = GetTask<Batch<PingMessage>>();
                Track(tcs.Task);
                var consumer = new TestBatchConsumer(tcs);
                return consumer;
            }, cc => cc.Options<BatchOptions>(x => x.SetTimeLimit(TimeSpan.FromMilliseconds(300)).GroupBy<PingMessage, Guid>(ctx => ctx.CorrelationId)));
        }
    }


    [TestFixture]
    public class Receiving_and_grouping_messages_by_ref_type :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_receive_one_batch_per_group()
        {
            var correlation1 = NewId.NextGuid();
            var correlation2 = NewId.NextGuid();

            await InputQueueSendEndpoint.Send(new PingMessage());
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation1));
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation1));
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation2));
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation2));
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.CorrelationId = correlation2));

            var count = await BusTestHarness.Consumed.SelectAsync<PingMessage>().Take(6).Count();

            Batch<PingMessage>[] batches = await CollectedBatches();

            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(6));
                Assert.That(batches.Select(x => x.Length), Is.EquivalentTo(new[] { 1, 2, 3 }));
            });
        }

        /// <summary>How many groups the six messages above fall into: one, two and three.</summary>
        const int ExpectedBatches = 3;

        readonly List<Task<Batch<PingMessage>>> _batches = new List<Task<Batch<PingMessage>>>();
        readonly TaskCompletionSource<bool> _allStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// The batches that were delivered, awaited rather than sampled.
        /// <para>
        /// Counting the six consumed messages says nothing about the batches: the last group's consumer
        /// can still be running when the sixth message is counted, and reading the list at that moment
        /// found two of three. The run then reported "observed &lt; 3, 1 &gt;, missing &lt; 2 &gt;" for a
        /// delivery that had in fact happened — the failure this fixture was labelled flaky for. Waiting
        /// for all three consumers to exist and then awaiting their batches removes the race without
        /// touching the assertion. No delay and no repetition: a race a sleep hides is still there.
        /// </para>
        /// </summary>
        async Task<Batch<PingMessage>[]> CollectedBatches()
        {
            await _allStarted.Task.OrTimeout(TimeSpan.FromSeconds(30));

            Task<Batch<PingMessage>>[] delivered;
            lock (_batches)
                delivered = _batches.ToArray();

            return await Task.WhenAll(delivered).OrTimeout(TimeSpan.FromSeconds(30));
        }

        /// <summary>Registers a batch consumer's delivery, and reports when the last group has one.</summary>
        void Track(Task<Batch<PingMessage>> batch)
        {
            lock (_batches)
            {
                _batches.Add(batch);

                if (_batches.Count == ExpectedBatches)
                    _allStarted.TrySetResult(true);
            }
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            configurator.ConcurrentMessageLimit = 10;

            configurator.Consumer(() =>
                {
                    TaskCompletionSource<Batch<PingMessage>> tcs = GetTask<Batch<PingMessage>>();
                    Track(tcs.Task);
                    var consumer = new TestBatchConsumer(tcs);
                    return consumer;
                },
                cc => cc.Options<BatchOptions>(x =>
                    x.SetTimeLimit(TimeSpan.FromMilliseconds(500)).GroupBy<PingMessage, string>(ctx => ctx.CorrelationId?.ToString("D"))));
        }
    }


    [TestFixture]
    public class Receiving_a_bunch_of_messages_in_a_batch_by_convention_using_mediator :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_receive_the_message_batch()
        {
            var consumer = new TestBatchConsumer(GetTask<Batch<PingMessage>>());

            var mediator = ViciOne.ServiceBus.Bus.Factory.CreateMediator(cfg =>
            {
                cfg.Consumer(() => consumer);
            });

            await Task.WhenAll(mediator.Send(new PingMessage()),
                mediator.Send(new PingMessage()),
                mediator.Send(new PingMessage()),
                mediator.Send(new PingMessage()));

            Batch<PingMessage> batch = await consumer.Completed;

            Assert.That(batch, Has.Length.EqualTo(4));
        }
    }


    [TestFixture]
    public class Using_a_batch_consumer :
        InMemoryTestFixture
    {
        /// <summary>
        /// Every message that was sent is delivered to the batch consumer exactly once.
        /// <para>
        /// A set of duplicates that stays empty is only half of that: a message the transport dropped
        /// never enters that set either, so a loss would pass and show up only as the fixture running
        /// into its own timeout. The identities are chosen by the sender and compared as sets, so a
        /// duplicate and a loss each fail on their own sentence.
        /// </para>
        /// <para>
        /// The delivery has to be exactly once while batches actually overlap, otherwise the case only
        /// describes a consumer that runs alone. Overlap is stated rather than provoked: each invocation
        /// announces that it is inside the consumer and waits until a second one has done the same, and
        /// the fixture asserts afterwards that this happened. A busy loop or a yield count can make
        /// overlap likely but cannot report whether it occurred.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_deliver_each_message_exactly_once()
        {
            Guid[] sent = Enumerable.Range(0, Count).Select(_ => NewId.NextGuid()).ToArray();

            foreach (var messageId in sent)
                await InputQueueSendEndpoint.Send(new DoWork(), Pipe.Execute<SendContext>(context => context.MessageId = messageId));

            // Completion alone cannot see a loss: it is raised once every identity has arrived, so a
            // message the transport dropped would leave this waiting until the fixture timed out and the
            // assertions below would never run. Inactivity is the second way out, and it is what turns a
            // loss into a failed sentence instead of a timeout.
            await Task.WhenAny(_completed.Task, InactivityTask);

            Guid[] received;
            Guid[] duplicates;
            lock (_alreadyReceivedMessages)
            {
                received = _alreadyReceivedMessages.ToArray();
                duplicates = _duplicateMessages.ToArray();
            }

            Assert.Multiple(() =>
            {
                Assert.That(_overlap.Observed, Is.True,
                    "no two batch consumer invocations were inside the consumer at the same time, so an exactly once delivery under overlap was never exercised");
                Assert.That(_overlap.HighWaterMark, Is.GreaterThanOrEqualTo(RequiredOverlap),
                    "fewer invocations overlapped than the case requires");
                Assert.That(duplicates, Is.Empty, "the batch consumer saw a message identity more than once");
                Assert.That(received, Is.EquivalentTo(sent), "the delivered identities are not exactly the sent ones");
            });
        }

        public Using_a_batch_consumer()
        {
            TestTimeout = TimeSpan.FromSeconds(30);
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
        }

        readonly HashSet<Guid> _alreadyReceivedMessages = new HashSet<Guid>();
        readonly HashSet<Guid> _duplicateMessages = new HashSet<Guid>();
        readonly OverlapBarrier _overlap = new OverlapBarrier(RequiredOverlap, TimeSpan.FromSeconds(10));
        TaskCompletionSource<int> _completed;

        /// <summary>How many invocations have to be inside the consumer together before any is released.</summary>
        const int RequiredOverlap = 2;

        /// <summary>Messages per batch.</summary>
        const int BatchSize = 100;

        /// <summary>How many batches the endpoint may deliver at the same time.</summary>
        const int ConcurrentBatches = 4;

        /// <summary>
        /// Enough messages for several batches to be in flight at once, and no more. Fifteen thousand
        /// stood here and proved nothing these ten batches do not.
        /// </summary>
        const int Count = 10 * BatchSize;

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _completed = GetTask<int>();

            // A message stays in flight until the batch it belongs to has been consumed, so the endpoint
            // needs room for every message of every batch that may overlap. With fewer slots than that
            // the configuration itself forbids the overlap this case is about.
            configurator.ConcurrentMessageLimit = BatchSize * ConcurrentBatches;

            configurator.Batch<DoWork>(x =>
            {
                x.MessageLimit = BatchSize;
                x.TimeLimit = TimeSpan.FromMilliseconds(50);
                x.ConcurrencyLimit = ConcurrentBatches;

                x.Consumer(() => new DoWorkConsumer(_alreadyReceivedMessages, _duplicateMessages, _completed, _overlap));
            });
        }


        public class DoWork
        {
        }


        /// <summary>
        /// Holds every arrival until the required number of them is inside, then releases all of them and
        /// stays open. Reached is completed by the arrivals themselves, so the release is an observation
        /// rather than a guess. The bound is what makes the barrier safe: an invocation that never gets
        /// company leaves anyway, and the fixture then fails on the assertion that no overlap happened
        /// instead of holding the endpoint for the rest of the run.
        /// </summary>
        class OverlapBarrier
        {
            readonly CancellationTokenSource _abandon;
            readonly int _required;
            readonly TaskCompletionSource<bool> _reached = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int _highWaterMark;
            int _inside;

            public OverlapBarrier(int required, TimeSpan bound)
            {
                _required = required;
                _abandon = new CancellationTokenSource(bound);
            }

            public bool Observed => _reached.Task.IsCompletedSuccessfully;

            public int HighWaterMark => Volatile.Read(ref _highWaterMark);

            public async Task Pass(CancellationToken cancellationToken)
            {
                var inside = Interlocked.Increment(ref _inside);

                int seen;
                do
                {
                    seen = Volatile.Read(ref _highWaterMark);
                }
                while (inside > seen && Interlocked.CompareExchange(ref _highWaterMark, inside, seen) != seen);

                if (inside >= _required)
                    _reached.TrySetResult(true);

                using var abandon = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _abandon.Token);

                try
                {
                    await _reached.Task.WaitAsync(abandon.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // No company arrived within the bound, or the fixture is shutting down. Leaving the
                    // consumer is the only useful thing left; the assertion on Observed reports it.
                }
                finally
                {
                    Interlocked.Decrement(ref _inside);
                }
            }
        }


        class DoWorkConsumer :
            IConsumer<Batch<DoWork>>
        {
            readonly HashSet<Guid> _alreadyReceivedMessages;
            readonly TaskCompletionSource<int> _completed;
            readonly HashSet<Guid> _duplicateMessages;
            readonly OverlapBarrier _overlap;

            public DoWorkConsumer(HashSet<Guid> alreadyReceivedMessages, HashSet<Guid> duplicateMessages, TaskCompletionSource<int> completed,
                OverlapBarrier overlap)
            {
                _alreadyReceivedMessages = alreadyReceivedMessages;
                _duplicateMessages = duplicateMessages;
                _completed = completed;
                _overlap = overlap;
            }

            public async Task Consume(ConsumeContext<Batch<DoWork>> context)
            {
                // Held here until another invocation is inside as well, so the recording below happens
                // while batches genuinely overlap rather than one after the other.
                await _overlap.Pass(context.CancellationToken).ConfigureAwait(false);

                lock (_alreadyReceivedMessages)
                {
                    foreach (ConsumeContext<DoWork> msg in context.Message)
                    {
                        if (_alreadyReceivedMessages.Contains(msg.MessageId.Value))
                        {
                            _duplicateMessages.Add(msg.MessageId.Value);

                            return;
                        }

                        _alreadyReceivedMessages.Add(msg.MessageId.Value);
                        if (_alreadyReceivedMessages.Count == Count)
                            _completed.TrySetResult(Count);
                    }
                }
            }
        }
    }


    [TestFixture]
    public class Duplicate_messages_by_id_consumer :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_receive_single_message_within_same_message_id()
        {
            var correlation1 = NewId.NextGuid();

            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.MessageId = correlation1));
            await InputQueueSendEndpoint.Send(new PingMessage(), Pipe.Execute<SendContext>(ctx => ctx.MessageId = correlation1));

            await InactivityTask;

            var count = await BusTestHarness.Consumed.SelectAsync<PingMessage>().Count();

            Assert.That(count, Is.EqualTo(1));
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            configurator.Consumer(() =>
            {
                TaskCompletionSource<Batch<PingMessage>> tcs = GetTask<Batch<PingMessage>>();
                return new TestBatchConsumer(tcs);
            }, cc => cc.Options<BatchOptions>(x =>
            {
                x.TimeLimit = TimeSpan.FromSeconds(1);
                x.MessageLimit = 2;
            }));
        }
    }


    class TestBatchConsumer :
        IConsumer<Batch<PingMessage>>
    {
        readonly TaskCompletionSource<Batch<PingMessage>> _messageTask;

        public TestBatchConsumer(TaskCompletionSource<Batch<PingMessage>> messageTask)
        {
            _messageTask = messageTask;
        }

        public Task<Batch<PingMessage>> Completed => _messageTask.Task;

        public Task Consume(ConsumeContext<Batch<PingMessage>> context)
        {
            _messageTask.TrySetResult(context.Message);

            return Task.CompletedTask;
        }
    }


    class FailingBatchConsumer :
        IConsumer<Batch<PingMessage>>
    {
        int _attempts;

        public int Attempts => _attempts;

        public Task Consume(ConsumeContext<Batch<PingMessage>> context)
        {
            Interlocked.Increment(ref _attempts);

            throw new Exception("some error");
        }
    }
}
