using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a message send topology pipe specification implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageSendTopologyPipeSpecification<TMessage> :
    ISpecificationPipeSpecification<SendContext<TMessage>>
    where TMessage : class
{
    readonly IMessageSendTopology<TMessage> _messageSendTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageSendTopology">The message send topology value.</param>
    public MessageSendTopologyPipeSpecification(IMessageSendTopology<TMessage> messageSendTopology)
    {
        _messageSendTopology = messageSendTopology;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(ISpecificationPipeBuilder<SendContext<TMessage>> builder)
    {
        var typeBuilder = new Builder(builder);

        _messageSendTopology.Apply(typeBuilder);
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
        ITopologyPipeBuilder<SendContext<TMessage>>
    {
        readonly ISpecificationPipeBuilder<SendContext<TMessage>> _builder;

        public Builder(ISpecificationPipeBuilder<SendContext<TMessage>> builder)
        {
            _builder = builder;
        }

        public void AddFilter(IFilter<SendContext<TMessage>> filter)
        {
            _builder.AddFilter(filter);
        }

        public bool IsDelegated => _builder.IsDelegated;
        public bool IsImplemented => _builder.IsImplemented;

        public ITopologyPipeBuilder<SendContext<TMessage>> CreateDelegatedBuilder()
        {
            return new ChildBuilder<SendContext<TMessage>>(this, IsImplemented, true);
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
