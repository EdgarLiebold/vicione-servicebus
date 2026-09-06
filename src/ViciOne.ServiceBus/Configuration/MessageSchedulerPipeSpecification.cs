using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for message scheduler pipe.</summary>
public class MessageSchedulerPipeSpecification :
    IPipeSpecification<ConsumeContext>
{
    readonly Uri _schedulerAddress;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="schedulerAddress">The scheduler address.</param>
    public MessageSchedulerPipeSpecification(Uri schedulerAddress)
    {
        _schedulerAddress = schedulerAddress;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new MessageSchedulerFilter(_schedulerAddress));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_schedulerAddress == null)
            yield return this.Failure("SchedulerAddress", "must not be null");
    }
}
