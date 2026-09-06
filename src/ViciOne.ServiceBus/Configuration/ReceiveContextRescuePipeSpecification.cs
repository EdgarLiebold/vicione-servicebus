using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Rescue;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for receive context rescue pipe.</summary>
public class ReceiveContextRescuePipeSpecification :
    ExceptionSpecification,
    IPipeSpecification<ReceiveContext>
{
    readonly IPipe<ExceptionReceiveContext> _rescuePipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="rescuePipe">The rescue pipe.</param>
    public ReceiveContextRescuePipeSpecification(IPipe<ExceptionReceiveContext> rescuePipe)
    {
        _rescuePipe = rescuePipe;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ReceiveContext> builder)
    {
        builder.AddFilter(new RescueFilter<ReceiveContext, ExceptionReceiveContext>(_rescuePipe, Filter,
            (context, ex) => new RescueExceptionReceiveContext(context, ex)));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_rescuePipe == null)
            yield return this.Failure("RescuePipe", "must not be null");
    }
}
