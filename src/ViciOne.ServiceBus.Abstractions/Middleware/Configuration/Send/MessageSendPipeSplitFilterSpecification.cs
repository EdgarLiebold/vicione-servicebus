using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for message send pipe split filter.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public class MessageSendPipeSplitFilterSpecification<TMessage, T> :
    ISpecificationPipeSpecification<SendContext<TMessage>>
    where TMessage : class
    where T : class
{
    readonly ISpecificationPipeSpecification<SendContext<T>> _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="specification">The specification.</param>
    public MessageSendPipeSplitFilterSpecification(ISpecificationPipeSpecification<SendContext<T>> specification)
    {
        _specification = specification;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(ISpecificationPipeBuilder<SendContext<TMessage>> builder)
    {
        var splitBuilder = new Builder(builder);

        _specification.Apply(splitBuilder);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }


    class Builder :
        ISpecificationPipeBuilder<SendContext<T>>
    {
        readonly ISpecificationPipeBuilder<SendContext<TMessage>> _builder;

        public Builder(ISpecificationPipeBuilder<SendContext<TMessage>> builder)
        {
            _builder = builder;
        }

        public void AddFilter(IFilter<SendContext<T>> filter)
        {
            var splitFilter = new SplitFilter<SendContext<TMessage>, SendContext<T>>(filter, ContextProvider, InputContextProvider);

            _builder.AddFilter(splitFilter);
        }

        public bool IsDelegated => _builder.IsDelegated;
        public bool IsImplemented => _builder.IsImplemented;

        public ISpecificationPipeBuilder<SendContext<T>> CreateDelegatedBuilder()
        {
            return new PipeConfigurator<SendContext<T>>.ChildSpecificationPipeBuilder(this, IsImplemented, true);
        }

        public ISpecificationPipeBuilder<SendContext<T>> CreateImplementedBuilder()
        {
            return new PipeConfigurator<SendContext<T>>.ChildSpecificationPipeBuilder(this, true, IsDelegated);
        }

        SendContext<TMessage> ContextProvider(SendContext<TMessage> context, SendContext<T> splitContext)
        {
            return context;
        }

        static SendContext<T> InputContextProvider(SendContext<TMessage> context)
        {
            return (SendContext<T>)context;
        }
    }
}
