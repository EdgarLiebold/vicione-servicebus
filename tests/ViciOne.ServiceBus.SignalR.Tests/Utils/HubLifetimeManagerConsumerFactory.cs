// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR.Tests.Utils
{
    using System;
    using System.Threading.Tasks;
    using Context;
    using Microsoft.AspNetCore.SignalR;


    public class HubLifetimeManagerConsumerFactory<TConsumer, THub> :
        IConsumerFactory<TConsumer>,
        IHubManagerConsumerFactory<THub>
        where TConsumer : class, IConsumer
        where THub : Hub
    {
        readonly Func<ViciOneServiceBusHubLifetimeManager<THub>, TConsumer> _factoryMethod;

        public HubLifetimeManagerConsumerFactory(Func<ViciOneServiceBusHubLifetimeManager<THub>, TConsumer> factoryMethod)
        {
            _factoryMethod = factoryMethod;
        }

        public async Task Send<TMessage>(ConsumeContext<TMessage> context, IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
            where TMessage : class
        {
            if (HubLifetimeManager == null)
                throw new ArgumentNullException(nameof(HubLifetimeManager));

            TConsumer consumer = null;
            try
            {
                consumer = _factoryMethod(HubLifetimeManager);

                if (consumer == null)
                    throw new ConsumerException($"Unable to resolve consumer type '{TypeCache<TConsumer>.ShortName}'.");

                await next.Send(new ConsumerConsumeContextScope<TConsumer, TMessage>(context, consumer)).ConfigureAwait(false);
            }
            finally
            {
                var disposable = consumer as IDisposable;
                disposable?.Dispose();
            }
        }

        public void Probe(ProbeContext context)
        {
            context.CreateConsumerFactoryScope<TConsumer>("hubLifetimeManagerFactory");
        }

        public ViciOneServiceBusHubLifetimeManager<THub> HubLifetimeManager { get; set; }
    }
}
