using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for in memory execute context outbox.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class InMemoryExecuteContextOutboxSpecification<TArguments> :
    IPipeSpecification<ExecuteContext<TArguments>>,
    IOutboxConfigurator
    where TArguments : class
{
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public InMemoryExecuteContextOutboxSpecification(IRegistrationContext context)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="setter">The setter.</param>
    public InMemoryExecuteContextOutboxSpecification(ISetScopedConsumeContext? setter)
    {
        _setter = setter;
    }

    /// <summary>Gets or sets the concurrent message delivery.</summary>
    public bool ConcurrentMessageDelivery { get; set; }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ExecuteContext<TArguments>> builder)
    {
        builder.AddFilter(
            new InMemoryOutboxFilter<ExecuteContext<TArguments>, InMemoryOutboxExecuteContext<TArguments>>(_setter, Factory, ConcurrentMessageDelivery));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    static InMemoryOutboxExecuteContext<TArguments> Factory(ExecuteContext<TArguments> context)
    {
        return new InMemoryOutboxExecuteContext<TArguments>(context);
    }
}
