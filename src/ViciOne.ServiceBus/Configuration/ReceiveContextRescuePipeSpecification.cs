using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Rescue;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the rescue branch for transport receive failures.</summary>
internal sealed class ReceiveContextRescuePipeSpecification :
    ExceptionSpecification,
    IPipeSpecification<ReceiveContext>
{
    readonly IPipe<ExceptionReceiveContext> _rescuePipe;

    /// <summary>Creates a specification for the supplied receive-failure pipe.</summary>
    /// <param name="rescuePipe">The pipe that handles selected receive failures.</param>
    public ReceiveContextRescuePipeSpecification(IPipe<ExceptionReceiveContext> rescuePipe)
    {
        _rescuePipe = rescuePipe ?? throw new ArgumentNullException(nameof(rescuePipe));
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ReceiveContext> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new RescueFilter<ReceiveContext, ExceptionReceiveContext>(_rescuePipe, Filter,
            (context, ex) => new RescueExceptionReceiveContext(context, ex)));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
