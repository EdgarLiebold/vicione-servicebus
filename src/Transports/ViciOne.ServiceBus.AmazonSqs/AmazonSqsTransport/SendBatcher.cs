using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a send batcher implementation.
/// </summary>
public class SendBatcher :
    Batcher<SendMessageBatchRequestEntry>
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
    /// <param name="settings">The settings value.</param>
    public SendBatcher(IAmazonSQS client, string queueUrl, CancellationToken cancellationToken, BatchSettings? settings = null)
        : base(settings)
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
    protected override int CalculateEntryLength(SendMessageBatchRequestEntry entry, string entryId)
    {
        entry.Id = entryId;

        var encoding = MessageDefaults.Encoding;

        return encoding.GetByteCount(entry.MessageBody)
            + (entry.MessageAttributes?.Where(x => x.Value.DataType == "String")
                .Sum(x => encoding.GetByteCount(x.Key) + encoding.GetByteCount(x.Value.StringValue))).GetValueOrDefault();
    }

    /// <summary>
    /// Sends batch.
    /// </summary>
    /// <param name="batch">The batch value.</param>
    /// <returns>The result of the operation.</returns>
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
