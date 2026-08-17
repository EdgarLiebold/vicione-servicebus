namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using ViciOne.ServiceBus.Middleware;
    using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
    using NUnit.Framework;
    using TestFramework;
    using TestFramework.Messages;


    /// <summary>
    /// What the outbox promises, stated as the order in which things happen: nothing pending is sent
    /// before the consume has completed, nothing pending is sent at all when the consume fails, and the
    /// scope context that was replaced for the duration is put back either way.
    /// <para>
    /// The filter is driven directly instead of through a bus, so the order is recorded rather than
    /// waited for. A test that published through a bus and then asserted that the message had "not
    /// arrived yet" would be measuring the clock, and would pass whenever the machine happened to be
    /// slower than the assertion.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_in_memory_outbox_lifecycle
    {
        [Test]
        public async Task Should_hold_a_pending_action_until_the_consume_has_completed()
        {
            var order = new List<string>();

            await Filter(null).Send(Context(), Pipe(async outbox =>
            {
                await outbox.Add(() =>
                {
                    order.Add("pending action ran");

                    return Task.CompletedTask;
                });

                order.Add("consume completed");
            }));

            Assert.That(order, Is.EqualTo(new[] { "consume completed", "pending action ran" }));
        }

        [Test]
        public void Should_discard_a_pending_action_when_the_consume_fails()
        {
            var order = new List<string>();

            Assert.That(async () => await Filter(null).Send(Context(), Pipe(async outbox =>
            {
                await outbox.Add(() =>
                {
                    order.Add("pending action ran");

                    return Task.CompletedTask;
                });

                order.Add("consume failed");

                throw new IntentionalTestException("the consume did not complete");
            })), Throws.TypeOf<IntentionalTestException>());

            Assert.That(order, Is.EqualTo(new[] { "consume failed" }), "a pending action was sent although the consume failed");
        }

        [Test]
        public async Task Should_restore_the_scope_context_after_the_pending_actions_ran()
        {
            var order = new List<string>();

            using var scope = new ServiceCollection().BuildServiceProvider().CreateScope();

            var context = Context();
            context.GetOrAddPayload<IServiceScope>(() => scope);

            await Filter(new RecordingSetter(order)).Send(context, Pipe(async outbox =>
            {
                await outbox.Add(() =>
                {
                    order.Add("pending action ran");

                    return Task.CompletedTask;
                });

                order.Add("consume completed");
            }));

            Assert.That(order, Is.EqualTo(new[]
            {
                "scope context replaced",
                "consume completed",
                "pending action ran",
                "scope context restored"
            }));
        }

        [Test]
        public void Should_restore_the_scope_context_even_when_the_consume_fails()
        {
            var order = new List<string>();

            using var scope = new ServiceCollection().BuildServiceProvider().CreateScope();

            var context = Context();
            context.GetOrAddPayload<IServiceScope>(() => scope);

            Assert.That(async () => await Filter(new RecordingSetter(order)).Send(context, Pipe(_ =>
            {
                order.Add("consume failed");

                throw new IntentionalTestException("the consume did not complete");
            })), Throws.TypeOf<IntentionalTestException>());

            Assert.That(order, Is.EqualTo(new[]
            {
                "scope context replaced",
                "consume failed",
                "scope context restored"
            }), "the replaced scope context was not put back after a failed consume");
        }

        static TestConsumeContext<PingMessage> Context()
        {
            return new TestConsumeContext<PingMessage>(new PingMessage());
        }

        static InMemoryOutboxFilter<ConsumeContext<PingMessage>, InMemoryOutboxConsumeContext<PingMessage>> Filter(ISetScopedConsumeContext setter)
        {
            return new InMemoryOutboxFilter<ConsumeContext<PingMessage>, InMemoryOutboxConsumeContext<PingMessage>>(
                setter, x => new InMemoryOutboxConsumeContext<PingMessage>(x), false);
        }

        static IPipe<ConsumeContext<PingMessage>> Pipe(Func<OutboxContext, Task> handle)
        {
            return new OutboxPipe(handle);
        }


        class OutboxPipe :
            IPipe<ConsumeContext<PingMessage>>
        {
            readonly Func<OutboxContext, Task> _handle;

            public OutboxPipe(Func<OutboxContext, Task> handle)
            {
                _handle = handle;
            }

            public Task Send(ConsumeContext<PingMessage> context)
            {
                return _handle((OutboxContext)context);
            }

            public void Probe(ProbeContext context)
            {
                context.CreateFilterScope("outbox-pipe");
            }
        }


        /// <summary>
        /// Stands in for the bus-bound setter and records when the scope context is replaced and when the
        /// replacement is undone, which is the part that is otherwise invisible from outside the filter.
        /// </summary>
        class RecordingSetter :
            ISetScopedConsumeContext
        {
            readonly List<string> _order;

            public RecordingSetter(List<string> order)
            {
                _order = order;
            }

            public IDisposable PushContext(IServiceScope serviceScope, ConsumeContext context)
            {
                _order.Add("scope context replaced");

                return new Restore(_order);
            }


            class Restore :
                IDisposable
            {
                readonly List<string> _order;

                public Restore(List<string> order)
                {
                    _order = order;
                }

                public void Dispose()
                {
                    _order.Add("scope context restored");
                }
            }
        }
    }
}
