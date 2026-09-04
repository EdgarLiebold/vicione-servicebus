using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Rescue;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer consume context rescue pipe specification implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ConsumerConsumeContextRescuePipeSpecification<T> :
    ExceptionSpecification,
    IPipeSpecification<ConsumerConsumeContext<T>>
    where T : class
{
    readonly IPipe<ExceptionConsumerConsumeContext<T>> _rescuePipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="rescuePipe">The rescue pipe value.</param>
    public ConsumerConsumeContextRescuePipeSpecification(IPipe<ExceptionConsumerConsumeContext<T>> rescuePipe)
    {
        _rescuePipe = rescuePipe;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<T>> builder)
    {
        builder.AddFilter(new RescueFilter<ConsumerConsumeContext<T>, ExceptionConsumerConsumeContext<T>>(_rescuePipe, Filter,
            (context, ex) => new RescueExceptionConsumerConsumeContext<T>(context, ex)));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_rescuePipe == null)
            yield return this.Failure("RescuePipe", "must not be null");
    }
}
