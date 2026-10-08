using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace ViciOne.ServiceBus.AmazonSqs;

class PublishBatcher :
    Batcher<PublishBatchRequestEntry>
{
    readonly CancellationToken _cancellationToken;
    readonly IAmazonSimpleNotificationService _client;
    readonly string _topicArn;

    public PublishBatcher(IAmazonSimpleNotificationService client, string topicArn, CancellationToken cancellationToken)
        : this(client, topicArn, cancellationToken, TimeProvider.System)
    {
    }

    internal PublishBatcher(IAmazonSimpleNotificationService client, string topicArn, CancellationToken cancellationToken,
        TimeProvider timeProvider)
        : base(PublishBatchSettings.GetBatchSettings(), timeProvider)
    {
        _client = client;
        _topicArn = topicArn;
        _cancellationToken = cancellationToken;
    }

    protected override void AssignEntryId(PublishBatchRequestEntry entry, string entryId) => entry.Id = entryId;

    protected override int CalculateEntryLength(PublishBatchRequestEntry entry) =>
        MessageDefaults.Encoding.GetByteCount(entry.Message)
        + AmazonMessageAttributeSizeCalculator.Calculate(entry.MessageAttributes);

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
