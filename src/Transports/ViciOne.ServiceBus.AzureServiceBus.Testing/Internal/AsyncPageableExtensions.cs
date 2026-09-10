using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;

namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

static class AsyncPageableExtensions
{
    public static async Task<IList<T>> ToListAsync<T>(this AsyncPageable<T> source, CancellationToken cancellationToken = default)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var items = new List<T>();
        await foreach (T item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            items.Add(item);

        return items;
    }
}
