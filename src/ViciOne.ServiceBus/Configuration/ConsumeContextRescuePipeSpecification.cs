using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Rescue;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for consume context rescue pipe.</summary>
public class ConsumeContextRescuePipeSpecification :
    ExceptionSpecification,
    IPipeSpecification<ConsumeContext>
{
    readonly IPipe<ExceptionConsumeContext> _rescuePipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="rescuePipe">The rescue pipe.</param>
    public ConsumeContextRescuePipeSpecification(IPipe<ExceptionConsumeContext> rescuePipe)
    {
        _rescuePipe = rescuePipe;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new RescueFilter<ConsumeContext, ExceptionConsumeContext>(_rescuePipe, Filter,
            (context, ex) => new RescueExceptionConsumeContext(context, ex)));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_rescuePipe == null)
            yield return this.Failure("RescuePipe", "must not be null");
    }
}


/// <summary>Describes requirements for consume context rescue pipe.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ConsumeContextRescuePipeSpecification<T> :
    ExceptionSpecification,
    IPipeSpecification<ConsumeContext<T>>
    where T : class
{
    readonly IPipe<ExceptionConsumeContext<T>> _rescuePipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="rescuePipe">The rescue pipe.</param>
    public ConsumeContextRescuePipeSpecification(IPipe<ExceptionConsumeContext<T>> rescuePipe)
    {
        _rescuePipe = rescuePipe;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        builder.AddFilter(new RescueFilter<ConsumeContext<T>, ExceptionConsumeContext<T>>(_rescuePipe, Filter,
            (context, ex) => new RescueExceptionConsumeContext<T>(context, ex)));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_rescuePipe == null)
            yield return this.Failure("RescuePipe", "must not be null");
    }
}
