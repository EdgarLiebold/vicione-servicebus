using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public class SendBatcher :
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

    protected override int CalculateEntryLength(SendMessageBatchRequestEntry entry, string entryId)
    {
        entry.Id = entryId;

        var encoding = MessageDefaults.Encoding;

        return encoding.GetByteCount(entry.MessageBody)
            + (entry.MessageAttributes?.Where(x => x.Value.DataType == "String")
                .Sum(x => encoding.GetByteCount(x.Key) + encoding.GetByteCount(x.Value.StringValue))).GetValueOrDefault();
    }

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
