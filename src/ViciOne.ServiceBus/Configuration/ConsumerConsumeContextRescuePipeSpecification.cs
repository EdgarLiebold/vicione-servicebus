using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Rescue;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the rescue branch for failures raised by one consumer instance.</summary>
/// <typeparam name="T">The consumer type.</typeparam>
internal sealed class ConsumerConsumeContextRescuePipeSpecification<T> :
    ExceptionSpecification,
    IPipeSpecification<ConsumerConsumeContext<T>>
    where T : class
{
    readonly IPipe<ExceptionConsumerConsumeContext<T>> _rescuePipe;

    /// <summary>Creates a specification for the supplied consumer-failure pipe.</summary>
    /// <param name="rescuePipe">The pipe that handles selected consumer failures.</param>
    public ConsumerConsumeContextRescuePipeSpecification(IPipe<ExceptionConsumerConsumeContext<T>> rescuePipe)
    {
        _rescuePipe = rescuePipe ?? throw new ArgumentNullException(nameof(rescuePipe));
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new RescueFilter<ConsumerConsumeContext<T>, ExceptionConsumerConsumeContext<T>>(_rescuePipe, Filter,
            (context, ex) => new RescueExceptionConsumerConsumeContext<T>(context, ex)));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
