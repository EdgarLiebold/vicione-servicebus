using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

class SendBatcher :
    Batcher<SendMessageBatchRequestEntry>
{
    readonly CancellationToken _cancellationToken;
    readonly IAmazonSQS _client;
    readonly string _queueUrl;

    public SendBatcher(IAmazonSQS client, string queueUrl, CancellationToken cancellationToken, BatchSettings? settings = null)
        : base(settings)
    {
        _client = client;
        _queueUrl = queueUrl;
        _cancellationToken = cancellationToken;
    }

    protected override void AssignEntryId(SendMessageBatchRequestEntry entry, string entryId) => entry.Id = entryId;

    protected override int CalculateEntryLength(SendMessageBatchRequestEntry entry) =>
        MessageDefaults.Encoding.GetByteCount(entry.MessageBody)
        + AmazonMessageAttributeSizeCalculator.Calculate(entry.MessageAttributes);

    protected override async Task SendBatchAsync(IList<BatchEntry<SendMessageBatchRequestEntry>> batch)
    {
        var batchRequest = new SendMessageBatchRequest(_queueUrl, batch.Select(x => x.Entry).ToList());

        var response = await _client.SendMessageBatchAsync(batchRequest, _cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();

        ApplyResponse(
            batch,
            response.Successful?.Select(x => x.Id),
            response.Failed?.Select(x => (x.Id, x.Code, x.Message)));
    }
}
