namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using ViciOne.ServiceBus.Middleware;
    using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
    using NUnit.Framework;
    using TestFramework;
    using TestFramework.Messages;


    /// <summary>
    /// The direct, container-less configuration has no registration context and therefore no bus-bound
    /// scoped consume context. It must not reach into somebody else's service provider for one.
    /// <para>
    /// The filter is driven directly rather than through a bus, because that is the only place where the
    /// statement is decidable. A foreign service scope wired onto the endpoint pipe stays green whichever
    /// implementation is in place: the outbox filter sits on the message-type pipe and never sees that
    /// payload, so the probe would prove nothing. Here the payload is on the very context the filter reads.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Running_the_in_memory_outbox_without_a_bus_bound_setter
    {
        [Test]
        public async Task Should_leave_a_foreign_service_scope_alone()
        {
            // A provider that knows nothing about messaging: no IScopedConsumeContextProvider is
            // registered, so anything that tries to resolve one from it fails loudly. That is exactly
            // what the previous implementation did on this path.
            await using var foreign = new ServiceCollection().BuildServiceProvider();
            using var scope = foreign.CreateScope();

            var context = new TestConsumeContext<PingMessage>(new PingMessage());
            context.GetOrAddPayload<IServiceScope>(() => scope);

            var consumed = false;

            var filter = new InMemoryOutboxFilter<ConsumeContext<PingMessage>, InMemoryOutboxConsumeContext<PingMessage>>(
                null, x => new InMemoryOutboxConsumeContext<PingMessage>(x), false);

            await filter.Send(context, new HandlerPipe(() => consumed = true));

            Assert.That(consumed, Is.True, "the message never reached the rest of the pipe");
        }

        [Test]
        public void Should_carry_the_scope_payload_on_the_context_the_filter_reads()
        {
            // The control for the case above. Without it a green result could mean the payload was never
            // there, which is how my earlier attempts were green for the wrong reason.
            using var scope = new ServiceCollection().BuildServiceProvider().CreateScope();

            var context = new TestConsumeContext<PingMessage>(new PingMessage());
            context.GetOrAddPayload<IServiceScope>(() => scope);

            Assert.That(context.TryGetPayload<IServiceScope>(out var found), Is.True);
            Assert.That(found, Is.SameAs(scope));
        }


        class HandlerPipe :
            IPipe<ConsumeContext<PingMessage>>
        {
            readonly Action _handle;

            public HandlerPipe(Action handle)
            {
                _handle = handle;
            }

            public Task Send(ConsumeContext<PingMessage> context)
            {
                _handle();

                return Task.CompletedTask;
            }

            public void Probe(ProbeContext context)
            {
                context.CreateFilterScope("handler");
            }
        }
    }
}
