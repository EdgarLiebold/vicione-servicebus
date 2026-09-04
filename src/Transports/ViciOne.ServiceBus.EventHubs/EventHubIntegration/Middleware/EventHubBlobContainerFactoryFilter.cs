using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;

namespace ViciOne.ServiceBus.EventHubs.Middleware;

/// <summary>
/// Provides an event hub blob container factory filter implementation.
/// </summary>
public class EventHubBlobContainerFactoryFilter :
    IFilter<ProcessorContext>
{
    readonly BlobContainerClient _blockClient;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="blockClient">The block client value.</param>
    public EventHubBlobContainerFactoryFilter(BlobContainerClient blockClient)
    {
        _blockClient = blockClient;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ProcessorContext context, IPipe<ProcessorContext> next)
    {
        OneTimeContext<EventHubBlobContainerFactoryFilter> oneTimeContext = await context
            .OneTimeSetupAsync<EventHubBlobContainerFactoryFilter>(() => CreateBlobIfNotExistsAsync(context.CancellationToken))
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

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");
        scope.Add("Uri", _blockClient.Uri);
        scope.Add("Name", _blockClient.Name);
    }

    async Task<bool> CreateBlobIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        Azure.Response<bool> exists = await _blockClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
        if (exists.Value)
            return true;

        try
        {
            await _blockClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (RequestFailedException exception)
        {
            LogContext.Warning?.Log(exception, "Azure Blob Container does not exist: {Address}", _blockClient.Uri);
            return false;
        }
    }
}
