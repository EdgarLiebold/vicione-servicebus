using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for in memory outbox.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class InMemoryOutboxSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>,
    IOutboxConfigurator
    where T : class
{
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public InMemoryOutboxSpecification(IRegistrationContext context)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="setter">The setter.</param>
    public InMemoryOutboxSpecification(ISetScopedConsumeContext? setter)
    {
        _setter = setter;
    }

    /// <summary>Gets or sets the concurrent message delivery.</summary>
    public bool ConcurrentMessageDelivery { get; set; }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        builder.AddFilter(new InMemoryOutboxFilter<ConsumeContext<T>, InMemoryOutboxConsumeContext<T>>(_setter, Factory, ConcurrentMessageDelivery));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    static InMemoryOutboxConsumeContext<T> Factory(ConsumeContext<T> context)
    {
        return new InMemoryOutboxConsumeContext<T>(context);
    }


    /// <summary>Applies in-memory outbox configuration to consumed message batches.</summary>
    public class Batch :
        IPipeSpecification<ConsumeContext<Batch<T>>>,
        IOutboxConfigurator
    {
        readonly ISetScopedConsumeContext? _setter;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="context">The context associated with the operation.</param>
        public Batch(IRegistrationContext context)
            : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
        {
        }

        /// <summary>Initializes a new instance.</summary>
        /// <param name="setter">The setter.</param>
        public Batch(ISetScopedConsumeContext? setter)
        {
            _setter = setter;
        }

        /// <summary>Gets or sets the concurrent message delivery.</summary>
        public bool ConcurrentMessageDelivery { get; set; }

        /// <summary>Validates the current configuration.</summary>
        /// <returns>The validation failures.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }

        /// <summary>Applies this specification to the target builder.</summary>
        /// <param name="builder">The builder that receives the configuration.</param>
        public void Apply(IPipeBuilder<ConsumeContext<Batch<T>>> builder)
        {
            builder.AddFilter(new InMemoryOutboxFilter<ConsumeContext<Batch<T>>, InMemoryOutboxConsumeContext<T>.Batch>(_setter, BatchFactory,
                ConcurrentMessageDelivery));
        }

        static InMemoryOutboxConsumeContext<T>.Batch BatchFactory(ConsumeContext<Batch<T>> context)
        {
            return new InMemoryOutboxConsumeContext<T>.Batch(context);
        }
    }
}
