using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Sends a failed receive to its configured error transport, then continues the exception pipeline.
/// </summary>
public class ErrorTransportFilter :
    IFilter<ExceptionReceiveContext>
{
    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("moveFault");
    }

    async Task IFilter<ExceptionReceiveContext>.SendAsync(ExceptionReceiveContext context, IPipe<ExceptionReceiveContext> next)
    {
        if (!context.TryGetPayload(out IErrorTransport? transport))
            throw new TransportException(context.InputAddress, $"The {nameof(IErrorTransport)} was not available on the {nameof(ReceiveContext)}.");

        await transport.SendAsync(context).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }
}
