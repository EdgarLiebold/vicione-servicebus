using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an in memory compensate context outbox specification implementation.
/// </summary>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class InMemoryCompensateContextOutboxSpecification<TArguments> :
    IPipeSpecification<CompensateContext<TArguments>>,
    IOutboxConfigurator
    where TArguments : class
{
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public InMemoryCompensateContextOutboxSpecification(IRegistrationContext context)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="setter">The setter value.</param>
    public InMemoryCompensateContextOutboxSpecification(ISetScopedConsumeContext? setter)
    {
        _setter = setter;
    }

    /// <summary>
    /// Gets or sets the concurrent message delivery value.
    /// </summary>
    public bool ConcurrentMessageDelivery { get; set; }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<CompensateContext<TArguments>> builder)
    {
        builder.AddFilter(
            new InMemoryOutboxFilter<CompensateContext<TArguments>, InMemoryOutboxCompensateContext<TArguments>>(_setter, Factory,
                ConcurrentMessageDelivery));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    static InMemoryOutboxCompensateContext<TArguments> Factory(CompensateContext<TArguments> context)
    {
        return new InMemoryOutboxCompensateContext<TArguments>(context);
    }
}
