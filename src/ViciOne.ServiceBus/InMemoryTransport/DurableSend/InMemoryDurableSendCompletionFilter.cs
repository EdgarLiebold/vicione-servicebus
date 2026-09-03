#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Completes a volatile durable send only after the full receive pipeline and all receive-owned tasks succeed.
/// </summary>
internal sealed class InMemoryDurableSendCompletionFilter : IFilter<ReceiveContext>
{
    public async Task Send(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        await next.Send(context).ConfigureAwait(false);

        if (!context.TryGetPayload(out InMemoryDurableSendContext? durableContext))
            return;

        await context.ReceiveCompleted.ConfigureAwait(false);
        await durableContext.ConsumerCompletion.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
    }

    public void Probe(ProbeContext context)
        => context.CreateFilterScope("inMemoryDurableSendCompletion");
}

internal sealed class InMemoryDurableSendCompletionPipeSpecification : IPipeSpecification<ReceiveContext>
{
    public void Apply(IPipeBuilder<ReceiveContext> builder)
        => builder.AddFilter(new InMemoryDurableSendCompletionFilter());

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
