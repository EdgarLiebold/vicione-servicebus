using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Rescue;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the rescue branch for consumed-message failures.</summary>
internal sealed class ConsumeContextRescuePipeSpecification :
    ExceptionSpecification,
    IPipeSpecification<ConsumeContext>
{
    readonly IPipe<ExceptionConsumeContext> _rescuePipe;

    /// <summary>Creates a specification for the supplied consume-failure pipe.</summary>
    /// <param name="rescuePipe">The pipe that handles selected consume failures.</param>
    public ConsumeContextRescuePipeSpecification(IPipe<ExceptionConsumeContext> rescuePipe)
    {
        _rescuePipe = rescuePipe ?? throw new ArgumentNullException(nameof(rescuePipe));
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new RescueFilter<ConsumeContext, ExceptionConsumeContext>(_rescuePipe, Filter,
            (context, ex) => new RescueExceptionConsumeContext(context, ex)));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}


/// <summary>Builds the rescue branch for failures of a typed consumed message.</summary>
/// <typeparam name="T">The consumed message type.</typeparam>
internal sealed class ConsumeContextRescuePipeSpecification<T> :
    ExceptionSpecification,
    IPipeSpecification<ConsumeContext<T>>
    where T : class
{
    readonly IPipe<ExceptionConsumeContext<T>> _rescuePipe;

    /// <summary>Creates a specification for the supplied typed consume-failure pipe.</summary>
    /// <param name="rescuePipe">The pipe that handles selected consume failures.</param>
    public ConsumeContextRescuePipeSpecification(IPipe<ExceptionConsumeContext<T>> rescuePipe)
    {
        _rescuePipe = rescuePipe ?? throw new ArgumentNullException(nameof(rescuePipe));
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new RescueFilter<ConsumeContext<T>, ExceptionConsumeContext<T>>(_rescuePipe, Filter,
            (context, ex) => new RescueExceptionConsumeContext<T>(context, ex)));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
