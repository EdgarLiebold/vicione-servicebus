using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a message consume topology pipe specification implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageConsumeTopologyPipeSpecification<TMessage> :
    ISpecificationPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly IMessageConsumeTopology<TMessage> _messageConsumeTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageConsumeTopology">The message consume topology value.</param>
    public MessageConsumeTopologyPipeSpecification(IMessageConsumeTopology<TMessage> messageConsumeTopology)
    {
        _messageConsumeTopology = messageConsumeTopology;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(ISpecificationPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        var typeBuilder = new Builder(builder);

        _messageConsumeTopology.Apply(typeBuilder);
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
        ITopologyPipeBuilder<ConsumeContext<TMessage>>
    {
        readonly IPipeBuilder<ConsumeContext<TMessage>> _builder;

        public Builder(IPipeBuilder<ConsumeContext<TMessage>> builder)
        {
            _builder = builder;
        }

        public void AddFilter(IFilter<ConsumeContext<TMessage>> filter)
        {
            _builder.AddFilter(filter);
        }

        public bool IsDelegated => false;
        public bool IsImplemented => false;

        public ITopologyPipeBuilder<ConsumeContext<TMessage>> CreateDelegatedBuilder()
        {
            return new ChildBuilder<ConsumeContext<TMessage>>(this, IsImplemented, true);
        }


        class ChildBuilder<T> :
            ITopologyPipeBuilder<T>
            where T : class, PipeContext
        {
            readonly ITopologyPipeBuilder<T> _builder;

            public ChildBuilder(ITopologyPipeBuilder<T> builder, bool isImplemented, bool isDelegated)
            {
                _builder = builder;

                IsDelegated = isDelegated;
                IsImplemented = isImplemented;
            }

            public void AddFilter(IFilter<T> filter)
            {
                _builder.AddFilter(filter);
            }

            public bool IsDelegated { get; }

            public bool IsImplemented { get; }

            public ITopologyPipeBuilder<T> CreateDelegatedBuilder()
            {
                return new ChildBuilder<T>(this, IsImplemented, true);
            }
        }
    }
}
