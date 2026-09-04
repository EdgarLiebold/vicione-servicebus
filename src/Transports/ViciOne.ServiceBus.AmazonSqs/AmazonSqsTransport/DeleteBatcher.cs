using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a delete batcher implementation.
/// </summary>
public class DeleteBatcher :
    Batcher<DeleteMessageBatchRequestEntry>
{
    readonly CancellationToken _cancellationToken;
    readonly IAmazonSQS _client;
    readonly string _queueUrl;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="client">The client value.</param>
    /// <param name="queueUrl">The queue url value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public DeleteBatcher(IAmazonSQS client, string queueUrl, CancellationToken cancellationToken)
    {
        _client = client;
        _queueUrl = queueUrl;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Performs the calculate entry length operation.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="entryId">The entry id value.</param>
    /// <returns>The result of the operation.</returns>
    protected override int CalculateEntryLength(DeleteMessageBatchRequestEntry entry, string entryId)
    {
        entry.Id = entryId;

        return 0;
    }

    /// <summary>
    /// Sends batch.
    /// </summary>
    /// <param name="batch">The batch value.</param>
    /// <returns>The result of the operation.</returns>
    protected override async Task SendBatchAsync(IList<BatchEntry<DeleteMessageBatchRequestEntry>> batch)
    {
        var batchRequest = new DeleteMessageBatchRequest(_queueUrl, batch.Select(x => x.Entry).ToList());

        var response = await _client.DeleteMessageBatchAsync(batchRequest, _cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();

        ApplyResponse(
            batch,
            response.Successful?.Select(x => x.Id),
            response.Failed?.Select(x => (x.Id, x.Code, x.Message)));
    }
}
