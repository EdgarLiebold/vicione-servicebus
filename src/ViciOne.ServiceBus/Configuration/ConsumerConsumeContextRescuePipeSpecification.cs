using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Rescue;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for consumer consume context rescue pipe.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ConsumerConsumeContextRescuePipeSpecification<T> :
    ExceptionSpecification,
    IPipeSpecification<ConsumerConsumeContext<T>>
    where T : class
{
    readonly IPipe<ExceptionConsumerConsumeContext<T>> _rescuePipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="rescuePipe">The rescue pipe.</param>
    public ConsumerConsumeContextRescuePipeSpecification(IPipe<ExceptionConsumerConsumeContext<T>> rescuePipe)
    {
        _rescuePipe = rescuePipe;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<T>> builder)
    {
        builder.AddFilter(new RescueFilter<ConsumerConsumeContext<T>, ExceptionConsumerConsumeContext<T>>(_rescuePipe, Filter,
            (context, ex) => new RescueExceptionConsumerConsumeContext<T>(context, ex)));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_rescuePipe == null)
            yield return this.Failure("RescuePipe", "must not be null");
    }
}
