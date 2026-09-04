using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Generates and publishes a <see cref="Fault" /> event for the exception
/// </summary>
public class GenerateFaultFilter :
    IFilter<ExceptionReceiveContext>
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("generateFault");
    }

    public async Task SendAsync(ExceptionReceiveContext context, IPipe<ExceptionReceiveContext> next)
    {
        if (!context.IsFaulted)
        {
            await GenerateFaultAsync(context).ConfigureAwait(false);

            await context.NotifyFaultedAsync(context.Exception).ConfigureAwait(false);
        }

        await next.SendAsync(context).ConfigureAwait(false);
    }

    static async Task GenerateFaultAsync(ExceptionReceiveContext context)
    {
        Guid? messageId;
        Guid? requestId;
        string[]? messageTypes = null;

        if (context.TryGetPayload(out ConsumeContext? consumeContext))
        {
            messageId = consumeContext.MessageId;
            requestId = consumeContext.RequestId;
            messageTypes = consumeContext.SupportedMessageTypes.ToArray();
        }
        else
        {
            messageId = context.GetMessageId();
            requestId = context.GetRequestId();
        }

        if (context.PublishFaults || consumeContext?.FaultAddress != null || consumeContext?.ResponseAddress != null)
        {
            ReceiveFault fault = new ReceiveFaultEvent(HostMetadataCache.Host, context.Exception, context.ContentType?.MediaType, messageId, messageTypes,
                context.GetTimeProvider());

            var faultEndpoint = await context.GetReceiveFaultEndpointAsync(consumeContext, requestId).ConfigureAwait(false);

            await faultEndpoint.SendAsync(fault).ConfigureAwait(false);
        }
    }
}
