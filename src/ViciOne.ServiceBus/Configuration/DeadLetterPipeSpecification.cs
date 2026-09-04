using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a dead letter pipe specification implementation.
/// </summary>
public class DeadLetterPipeSpecification :
    IPipeSpecification<ReceiveContext>
{
    readonly IPipe<ReceiveContext> _deadLetterPipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="deadLetterPipe">The dead letter pipe value.</param>
    public DeadLetterPipeSpecification(IPipe<ReceiveContext> deadLetterPipe)
    {
        _deadLetterPipe = deadLetterPipe;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ReceiveContext> builder)
    {
        builder.AddFilter(new DeadLetterFilter(_deadLetterPipe));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_deadLetterPipe == null)
            yield return this.Failure("RescuePipe", "must not be null");
    }
}
