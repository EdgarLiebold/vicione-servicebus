using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace ViciOne.ServiceBus.EventHubs.Middleware;

/// <summary>Performs one-time Blob checkpoint-container setup before the Event Hubs processor pipeline continues.</summary>
public class EventHubBlobContainerFactoryFilter :
    IFilter<ProcessorContext>
{
    readonly BlobContainerClient _blobContainerClient;

    /// <summary>Creates the filter for the configured checkpoint container.</summary>
    /// <param name="blobContainerClient">The Blob container client used for existence checks and creation.</param>
    public EventHubBlobContainerFactoryFilter(BlobContainerClient blobContainerClient)
    {
        _blobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
    }

    /// <summary>Runs one-time container setup, invokes the processor pipeline, and evicts setup state when downstream processing faults.</summary>
    /// <param name="context">The active Event Hubs processor context.</param>
    /// <param name="next">The remaining processor pipeline.</param>
    /// <returns>The continuation task returned by <paramref name="next"/> after the checkpoint container is available.</returns>
    public async Task SendAsync(ProcessorContext context, IPipe<ProcessorContext> next)
    {
        OneTimeContext<EventHubBlobContainerFactoryFilter> oneTimeContext = await context
            .OneTimeSetupAsync<EventHubBlobContainerFactoryFilter>(() => EnsureContainerExistsAsync(context.CancellationToken))
            .ConfigureAwait(false);

        try
        {
            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception)
        {
            oneTimeContext.Evict();
            throw;
        }
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context receiving checkpoint-container URI and name.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");
        scope.Add("Uri", _blobContainerClient.Uri);
        scope.Add("Name", _blobContainerClient.Name);
    }

    /// <summary>Creates the checkpoint container when it does not already exist.</summary>
    /// <param name="cancellationToken">Cancels the existence check or container creation.</param>
    /// <returns>A task that completes only after the container is known to exist.</returns>
    internal async Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
    {
        global::Azure.Response<bool> exists = await _blobContainerClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        if (exists.Value)
            return;

        await _blobContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
