using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.DurableSend;

/// <summary>Adds in-memory durable-send consumer completion to a receive pipeline.</summary>
internal sealed class InMemoryDurableSendCompletionPipeSpecification : IPipeSpecification<ReceiveContext>
{
    public void Apply(IPipeBuilder<ReceiveContext> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new InMemoryDurableSendCompletionFilter());
    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
