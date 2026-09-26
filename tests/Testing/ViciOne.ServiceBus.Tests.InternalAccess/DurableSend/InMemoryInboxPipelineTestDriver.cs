using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;

/// <summary>Invokes the registered in-memory inbox pipeline with a controlled consumer operation.</summary>
public static class InMemoryInboxPipelineTestDriver
{
    public static Task SendAsync<T>(
        IServiceProvider provider,
        ConsumeContext<T> context,
        Guid consumerId,
        Func<ConsumeContext<T>, Task> consume,
        CancellationToken cancellationToken)
        where T : class
    {
        var factory = provider.GetRequiredService<IOutboxContextFactory<InMemoryReliableInboxScope<IBus>>>();
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = "cancellation-probe",
            MessageDeliveryLimit = 1,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        return factory.SendAsync(context, options,
            Pipe.ExecuteAwaited<OutboxConsumeContext<T>>(outbox => consume(outbox)), cancellationToken);
    }
}
