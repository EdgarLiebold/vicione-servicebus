#nullable enable

namespace ViciOne.ServiceBus.DurableSend;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

internal static class DurableSenderComposition
{
    public static T RequireExactlyOne<T, TBus>(IEnumerable<T> components, string component)
        where T : class
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(components);
        T[] snapshot = components.Take(2).ToArray();
        return snapshot.Length switch
        {
            1 => snapshot[0],
            0 => throw new ConfigurationException(
                $"Durable Sender for bus '{typeof(TBus)}' has no {component}. Configure it inside the owning bus.UseDurableSender(...) block."),
            _ => throw new ConfigurationException(
                $"Durable Sender for bus '{typeof(TBus)}' has multiple {component} owners. Configure exactly one."),
        };
    }
}

/// <summary>Fails static composition during host startup, before the delivery background loop begins.</summary>
internal sealed class DurableSenderStartupValidator<TBus> : IHostedService
    where TBus : class, IBus
{
    public DurableSenderStartupValidator(
        IEnumerable<TBus> buses,
        IEnumerable<IMessageContractCatalog> catalogs,
        IEnumerable<IDurableSendStore<TBus>> stores,
        IEnumerable<IDurableSendDispatcher<TBus>> dispatchers)
    {
        _ = DurableSenderComposition.RequireExactlyOne<TBus, TBus>(buses, "typed bus instance");
        _ = DurableSenderComposition.RequireExactlyOne<IMessageContractCatalog, TBus>(catalogs, "message-contract catalog");
        _ = DurableSenderComposition.RequireExactlyOne<IDurableSendStore<TBus>, TBus>(stores, "persistence store");
        _ = DurableSenderComposition.RequireExactlyOne<IDurableSendDispatcher<TBus>, TBus>(dispatchers, "transport dispatcher");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
