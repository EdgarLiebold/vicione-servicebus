using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a publish batcher implementation.
/// </summary>
public class PublishBatcher :
    Batcher<PublishBatchRequestEntry>
{
    readonly CancellationToken _cancellationToken;
    readonly IAmazonSimpleNotificationService _client;
    readonly string _topicArn;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="client">The client value.</param>
    /// <param name="topicArn">The topic arn value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public PublishBatcher(IAmazonSimpleNotificationService client, string topicArn, CancellationToken cancellationToken)
        : base(PublishBatchSettings.GetBatchSettings())
    {
        _client = client;
        _topicArn = topicArn;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Performs the calculate entry length operation.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="entryId">The entry id value.</param>
    /// <returns>The result of the operation.</returns>
    protected override int CalculateEntryLength(PublishBatchRequestEntry entry, string entryId)
    {
        entry.Id = entryId;

        var encoding = MessageDefaults.Encoding;

        return encoding.GetByteCount(entry.Message)
            + (entry.MessageAttributes?.Where(x => x.Value.DataType == "String")
                .Sum(x => encoding.GetByteCount(x.Key) + encoding.GetByteCount(x.Value.StringValue))).GetValueOrDefault();
    }

    /// <summary>
    /// Sends batch.
    /// </summary>
    /// <param name="batch">The batch value.</param>
    /// <returns>The result of the operation.</returns>
    protected override async Task SendBatchAsync(IList<BatchEntry<PublishBatchRequestEntry>> batch)
    {
        var batchRequest = new PublishBatchRequest
        {
            TopicArn = _topicArn,
            PublishBatchRequestEntries = batch.Select(x => x.Entry).ToList()
        };

        var response = await _client.PublishBatchAsync(batchRequest, _cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessfulResponse();

        ApplyResponse(
            batch,
            response.Successful?.Select(x => x.Id),
            response.Failed?.Select(x => (x.Id, x.Code, x.Message)));
    }
}
