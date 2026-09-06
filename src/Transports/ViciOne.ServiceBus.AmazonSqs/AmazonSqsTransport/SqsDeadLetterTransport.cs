using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Moves skipped messages to their configured Amazon SQS destination.</summary>
public class SqsDeadLetterTransport :
    SqsMoveTransport<DeadLetterSettings>,
    IDeadLetterTransport
{
    readonly TransportSetHeaderAdapter<MessageAttributeValue> _headerAdapter;

    /// <summary>Initializes an Amazon SQS skipped-message transport.</summary>
    /// <param name="destination">The logical destination queue name.</param>
    /// <param name="headerAdapter">The adapter used to add transport headers.</param>
    /// <param name="topologyFilter">The filter that declares the destination topology.</param>
    public SqsDeadLetterTransport(string destination, TransportSetHeaderAdapter<MessageAttributeValue> headerAdapter,
        ConfigureAmazonSqsTopologyFilter<DeadLetterSettings> topologyFilter)
        : base(destination, topologyFilter)
    {
        _headerAdapter = headerAdapter;
    }

    /// <summary>Moves a received message after adding its skipped-message reason.</summary>
    /// <param name="context">The received message context.</param>
    /// <param name="reason">The reason the message was skipped.</param>
    /// <param name="cancellationToken">The caller token used to cancel topology declaration and provider submission.</param>
    /// <returns>A task that completes when the message has been sent to the skipped-message queue.</returns>
    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        void PreSend(SendMessageBatchRequestEntry entry, IDictionary<string, MessageAttributeValue> headers)
        {
            _headerAdapter.Set(headers, MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend, cancellationToken);
    }
}
