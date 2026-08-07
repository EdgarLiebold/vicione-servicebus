// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using TestFramework;
    using TestFramework.Messages;


    [TestFixture]
    public class Using_a_request_filter_with_request_client
    {
        [Test]
        public async Task Should_fault_instead_of_timeout()
        {
            await using var provider = new ServiceCollection()
                .AddViciOneServiceBusTestHarness(x =>
                {
                    x.AddConsumer<PingConsumer>();

                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.UseConsumeFilter(typeof(RequestValidationScopedFilter<>), context);

                        cfg.ConfigureEndpoints(context);
                    });
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            IRequestClient<PingMessage> client = harness.GetRequestClient<PingMessage>();

            Assert.That(async () =>
                await client.GetResponse<PongMessage>(new
                {
                    CorrelationId = InVar.Id,
                }), Throws.TypeOf<RequestFaultException>());
        }


        public class PingConsumer :
            IConsumer<PingMessage>
        {
            public Task Consume(ConsumeContext<PingMessage> context)
            {
                return context.RespondAsync(new PongMessage(context.Message.CorrelationId));
            }
        }


        public class RequestValidationScopedFilter<T> :
            IFilter<ConsumeContext<T>>
            where T : class
        {
            public void Probe(ProbeContext context)
            {
                context.CreateFilterScope("RequestValidationScopedFilter<TMessage> scope");
            }

            public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
            {
                try
                {
                    throw new IntentionalTestException("Failed to validate request");
                }
                catch (Exception exception)
                {
                    await context.NotifyFaulted(context.ReceiveContext.ElapsedTime, TypeCache<RequestValidationScopedFilter<T>>.ShortName, exception)
                        .ConfigureAwait(false);

                    throw;
                }
            }
        }
    }
}
