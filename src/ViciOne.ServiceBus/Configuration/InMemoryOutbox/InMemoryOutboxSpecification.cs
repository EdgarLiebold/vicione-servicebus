using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures an in-memory outbox around a consumed-message pipeline.</summary>
/// <typeparam name="T">The consumed message contract.</typeparam>
public class InMemoryOutboxSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>,
    IOutboxConfigurator
    where T : class
{
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>Creates a specification using the registration context's scoped consume-context accessor.</summary>
    /// <param name="context">The registration context that supplies the accessor.</param>
    public InMemoryOutboxSpecification(IRegistrationContext context)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
    {
    }

    /// <summary>Creates a specification with an optional scoped consume-context accessor.</summary>
    /// <param name="setter">The accessor used to expose the active context inside a dependency-injection scope.</param>
    public InMemoryOutboxSpecification(ISetScopedConsumeContext? setter)
    {
        _setter = setter;
    }

    /// <summary>Gets or sets whether independent buffered operations may be delivered concurrently.</summary>
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
    internal sealed class BatchSpecification :
        IPipeSpecification<ConsumeContext<IMessageBatch<T>>>,
        IOutboxConfigurator
    {
        readonly ISetScopedConsumeContext? _setter;

        /// <summary>Creates a batch specification using the registration context's scoped consume-context accessor.</summary>
        /// <param name="context">The registration context that supplies the accessor.</param>
        public BatchSpecification(IRegistrationContext context)
            : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)))
        {
        }

        /// <summary>Creates a batch specification with an optional scoped consume-context accessor.</summary>
        /// <param name="setter">The accessor used to expose active contexts inside dependency-injection scopes.</param>
        public BatchSpecification(ISetScopedConsumeContext? setter)
        {
            _setter = setter;
        }

        /// <summary>Gets or sets whether independent buffered operations may be delivered concurrently.</summary>
        public bool ConcurrentMessageDelivery { get; set; }

        /// <summary>Validates the current configuration.</summary>
        /// <returns>The validation failures.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }

        /// <summary>Applies this specification to the target builder.</summary>
        /// <param name="builder">The builder that receives the configuration.</param>
        public void Apply(IPipeBuilder<ConsumeContext<IMessageBatch<T>>> builder)
        {
            builder.AddFilter(new InMemoryOutboxFilter<ConsumeContext<IMessageBatch<T>>, InMemoryOutboxConsumeContext<T>.BatchContext>(_setter, BatchFactory,
                ConcurrentMessageDelivery));
        }

        static InMemoryOutboxConsumeContext<T>.BatchContext BatchFactory(ConsumeContext<IMessageBatch<T>> context)
        {
            return new InMemoryOutboxConsumeContext<T>.BatchContext(context);
        }
    }
}
