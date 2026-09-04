using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a message publish pipe split filter specification implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public class MessagePublishPipeSplitFilterSpecification<TMessage, T> :
    ISpecificationPipeSpecification<PublishContext<TMessage>>
    where TMessage : class
    where T : class
{
    readonly ISpecificationPipeSpecification<PublishContext<T>> _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public MessagePublishPipeSplitFilterSpecification(ISpecificationPipeSpecification<PublishContext<T>> specification)
    {
        _specification = specification;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(ISpecificationPipeBuilder<PublishContext<TMessage>> builder)
    {
        var splitBuilder = new Builder(builder);

        _specification.Apply(splitBuilder);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }


    class Builder :
        ISpecificationPipeBuilder<PublishContext<T>>
    {
        readonly ISpecificationPipeBuilder<PublishContext<TMessage>> _builder;

        public Builder(ISpecificationPipeBuilder<PublishContext<TMessage>> builder)
        {
            _builder = builder;
        }

        public void AddFilter(IFilter<PublishContext<T>> filter)
        {
            var splitFilter = new SplitFilter<PublishContext<TMessage>, PublishContext<T>>(filter, ContextProvider, InputContextProvider);

            _builder.AddFilter(splitFilter);
        }

        public bool IsDelegated => _builder.IsDelegated;
        public bool IsImplemented => _builder.IsImplemented;

        public ISpecificationPipeBuilder<PublishContext<T>> CreateDelegatedBuilder()
        {
            return new PipeConfigurator<PublishContext<T>>.ChildSpecificationPipeBuilder(this, IsImplemented, true);
        }

        public ISpecificationPipeBuilder<PublishContext<T>> CreateImplementedBuilder()
        {
            return new PipeConfigurator<PublishContext<T>>.ChildSpecificationPipeBuilder(this, true, IsDelegated);
        }

        PublishContext<TMessage> ContextProvider(PublishContext<TMessage> context, PublishContext<T> splitContext)
        {
            return context;
        }

        static PublishContext<T> InputContextProvider(PublishContext<TMessage> context)
        {
            return (PublishContext<T>)context;
        }
    }
}
