using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

sealed class DeleteBatcher :
    Batcher<DeleteMessageBatchRequestEntry>
{
    readonly CancellationToken _cancellationToken;
    readonly IAmazonSQS _client;
    readonly string _queueUrl;

    public DeleteBatcher(IAmazonSQS client, string queueUrl, CancellationToken cancellationToken)
    {
        _client = client;
        _queueUrl = queueUrl;
        _cancellationToken = cancellationToken;
    }

    protected override void AssignEntryId(DeleteMessageBatchRequestEntry entry, string entryId) => entry.Id = entryId;

    protected override int CalculateEntryLength(DeleteMessageBatchRequestEntry entry) => 0;

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
