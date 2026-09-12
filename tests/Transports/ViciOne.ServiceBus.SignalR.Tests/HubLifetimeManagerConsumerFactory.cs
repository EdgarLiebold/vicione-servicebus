using Microsoft.AspNetCore.SignalR;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.SignalR.Runtime;

namespace ViciOne.ServiceBus.SignalR.Tests;

internal sealed class HubLifetimeManagerConsumerFactory<TConsumer, THub>(
    Func<ServiceBusHubLifetimeManager<THub>, TConsumer> factory)
    : IConsumerFactory<TConsumer>, IHubLifetimeManagerConsumerFactory<THub>
    where TConsumer : class, IConsumer
    where THub : Hub
{
    public ServiceBusHubLifetimeManager<THub> Manager { private get; set; } = null!;

    public async Task SendAsync<TMessage>(
        ConsumeContext<TMessage> context,
        IPipe<ConsumerConsumeContext<TConsumer, TMessage>> next)
        where TMessage : class
    {
        TConsumer consumer = factory(Manager)
            ?? throw new ConsumerException($"Unable to create consumer '{TypeCache<TConsumer>.ShortName}'.");

        try
        {
            await next.SendAsync(new ConsumerContext<TMessage>(context, consumer))
                .ConfigureAwait(false);
        }
        finally
        {
            (consumer as IDisposable)?.Dispose();
        }
    }

    public void Probe(ProbeContext context) =>
        context.CreateConsumerFactoryScope<TConsumer>("signalRBackplaneFactory");

    private sealed class ConsumerContext<TMessage>(ConsumeContext<TMessage> context, TConsumer consumer) :
        ConsumeContextScope<TMessage>(context),
        ConsumerConsumeContext<TConsumer, TMessage>
        where TMessage : class
    {
        public TConsumer Consumer { get; } = consumer;
    }
}
