using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for dead letter pipe.</summary>
public class DeadLetterPipeSpecification :
    IPipeSpecification<ReceiveContext>
{
    readonly IPipe<ReceiveContext> _deadLetterPipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="deadLetterPipe">The dead letter pipe.</param>
    public DeadLetterPipeSpecification(IPipe<ReceiveContext> deadLetterPipe)
    {
        _deadLetterPipe = deadLetterPipe;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ReceiveContext> builder)
    {
        builder.AddFilter(new DeadLetterFilter(_deadLetterPipe));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_deadLetterPipe == null)
            yield return this.Failure("RescuePipe", "must not be null");
    }
}
