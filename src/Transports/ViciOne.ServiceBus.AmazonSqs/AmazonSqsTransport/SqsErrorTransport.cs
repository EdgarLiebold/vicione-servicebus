using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a sqs error transport implementation.
/// </summary>
public class SqsErrorTransport :
    SqsMoveTransport<ErrorSettings>,
    IErrorTransport
{
    readonly ITransportSetHeaderAdapter<MessageAttributeValue> _headerAdapter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="headerAdapter">The header adapter value.</param>
    /// <param name="topologyFilter">The topology filter value.</param>
    public SqsErrorTransport(string destination, ITransportSetHeaderAdapter<MessageAttributeValue> headerAdapter,
        ConfigureAmazonSqsTopologyFilter<ErrorSettings> topologyFilter)
        : base(destination, topologyFilter)
    {
        _headerAdapter = headerAdapter;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(SendMessageBatchRequestEntry entry, IDictionary<string, MessageAttributeValue> headers)
        {
            _headerAdapter.CopyFrom(headers, context.ExceptionHeaders);
        }

        return MoveAsync(context, PreSend);
    }
}
