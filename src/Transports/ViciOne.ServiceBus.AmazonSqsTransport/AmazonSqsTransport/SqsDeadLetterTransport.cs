using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqsTransport.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public class SqsDeadLetterTransport :
    SqsMoveTransport<DeadLetterSettings>,
    IDeadLetterTransport
{
    readonly TransportSetHeaderAdapter<MessageAttributeValue> _headerAdapter;

    public SqsDeadLetterTransport(string destination, TransportSetHeaderAdapter<MessageAttributeValue> headerAdapter,
        ConfigureAmazonSqsTopologyFilter<DeadLetterSettings> topologyFilter)
        : base(destination, topologyFilter)
    {
        _headerAdapter = headerAdapter;
    }

    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(SendMessageBatchRequestEntry entry, IDictionary<string, MessageAttributeValue> headers)
        {
            _headerAdapter.Set(headers, MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
