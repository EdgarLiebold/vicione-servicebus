using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for message publish topology pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessagePublishTopologyPipeSpecification<TMessage> :
    ISpecificationPipeSpecification<PublishContext<TMessage>>
    where TMessage : class
{
    readonly IMessagePublishTopology<TMessage> _messagePublishTopology;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messagePublishTopology">The message publish topology.</param>
    public MessagePublishTopologyPipeSpecification(IMessagePublishTopology<TMessage> messagePublishTopology)
    {
        _messagePublishTopology = messagePublishTopology;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(ISpecificationPipeBuilder<PublishContext<TMessage>> builder)
    {
        var typeBuilder = new Builder(builder);

        _messagePublishTopology.Apply(typeBuilder);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }


    class Builder :
        ITopologyPipeBuilder<PublishContext<TMessage>>
    {
        readonly ISpecificationPipeBuilder<PublishContext<TMessage>> _builder;

        public Builder(ISpecificationPipeBuilder<PublishContext<TMessage>> builder)
        {
            _builder = builder;
        }

        public void AddFilter(IFilter<PublishContext<TMessage>> filter)
        {
            _builder.AddFilter(filter);
        }

        public bool IsDelegated => _builder.IsDelegated;
        public bool IsImplemented => _builder.IsImplemented;

        public ITopologyPipeBuilder<PublishContext<TMessage>> CreateDelegatedBuilder()
        {
            return new ChildBuilder<PublishContext<TMessage>>(this, IsImplemented, true);
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
