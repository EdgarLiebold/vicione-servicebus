using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an in memory outbox specification implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class InMemoryOutboxSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>,
    IOutboxConfigurator
    where T : class
{
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public InMemoryOutboxSpecification(IRegistrationContext context)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="setter">The setter value.</param>
    public InMemoryOutboxSpecification(ISetScopedConsumeContext? setter)
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
    public void Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        builder.AddFilter(new InMemoryOutboxFilter<ConsumeContext<T>, InMemoryOutboxConsumeContext<T>>(_setter, Factory, ConcurrentMessageDelivery));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    static InMemoryOutboxConsumeContext<T> Factory(ConsumeContext<T> context)
    {
        return new InMemoryOutboxConsumeContext<T>(context);
    }


    /// <summary>
    /// Provides a batch implementation.
    /// </summary>
    public class Batch :
        IPipeSpecification<ConsumeContext<Batch<T>>>,
        IOutboxConfigurator
    {
        readonly ISetScopedConsumeContext? _setter;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="context">The operation context.</param>
        public Batch(IRegistrationContext context)
            : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="setter">The setter value.</param>
        public Batch(ISetScopedConsumeContext? setter)
        {
            _setter = setter;
        }

        /// <summary>
        /// Gets or sets the concurrent message delivery value.
        /// </summary>
        public bool ConcurrentMessageDelivery { get; set; }

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }

        /// <summary>
        /// Applies this specification to the target builder.
        /// </summary>
        /// <param name="builder">The builder value.</param>
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
