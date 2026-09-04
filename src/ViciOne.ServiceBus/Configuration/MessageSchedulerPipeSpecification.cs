using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a message scheduler pipe specification implementation.
/// </summary>
public class MessageSchedulerPipeSpecification :
    IPipeSpecification<ConsumeContext>
{
    readonly Uri _schedulerAddress;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedulerAddress">The scheduler address value.</param>
    public MessageSchedulerPipeSpecification(Uri schedulerAddress)
    {
        _schedulerAddress = schedulerAddress;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new MessageSchedulerFilter(_schedulerAddress));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_schedulerAddress == null)
            yield return this.Failure("SchedulerAddress", "must not be null");
    }
}
