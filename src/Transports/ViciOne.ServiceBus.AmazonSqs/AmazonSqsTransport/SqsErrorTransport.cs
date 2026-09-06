using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Moves faulted messages to their configured Amazon SQS destination.</summary>
public class SqsErrorTransport :
    SqsMoveTransport<ErrorSettings>,
    IErrorTransport
{
    readonly ITransportSetHeaderAdapter<MessageAttributeValue> _headerAdapter;

    /// <summary>Initializes an Amazon SQS faulted-message transport.</summary>
    /// <param name="destination">The logical destination queue name.</param>
    /// <param name="headerAdapter">The adapter used to add exception headers.</param>
    /// <param name="topologyFilter">The filter that declares the destination topology.</param>
    public SqsErrorTransport(string destination, ITransportSetHeaderAdapter<MessageAttributeValue> headerAdapter,
        ConfigureAmazonSqsTopologyFilter<ErrorSettings> topologyFilter)
        : base(destination, topologyFilter)
    {
        _headerAdapter = headerAdapter;
    }

    /// <summary>Moves a faulted message after copying its exception headers.</summary>
    /// <param name="context">The faulted receive context.</param>
    /// <param name="cancellationToken">The caller token used to cancel topology declaration and provider submission.</param>
    /// <returns>A task that completes when the message has been sent to the error queue.</returns>
    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        void PreSend(SendMessageBatchRequestEntry entry, IDictionary<string, MessageAttributeValue> headers)
        {
            _headerAdapter.CopyFrom(headers, context.ExceptionHeaders);
        }

        return MoveAsync(context, PreSend, cancellationToken);
    }
}
