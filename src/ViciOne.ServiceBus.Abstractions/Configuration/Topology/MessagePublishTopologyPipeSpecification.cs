using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies the publish topology for a message contract to a publish pipe.</summary>
/// <typeparam name="TMessage">The published message contract type.</typeparam>
public sealed class MessagePublishTopologyPipeSpecification<TMessage> :
    ISpecificationPipeSpecification<PublishContext<TMessage>>
    where TMessage : class
{
    readonly IMessagePublishTopology<TMessage> _messagePublishTopology;

    /// <summary>Initializes the specification with the topology to apply.</summary>
    /// <param name="messagePublishTopology">The publish-message topology.</param>
    public MessagePublishTopologyPipeSpecification(IMessagePublishTopology<TMessage> messagePublishTopology)
    {
        _messagePublishTopology = messagePublishTopology ?? throw new ArgumentNullException(nameof(messagePublishTopology));
    }

    /// <summary>Applies the configured message topology to a publish-pipe builder.</summary>
    /// <param name="builder">The publish-pipe builder.</param>
    public void Apply(ISpecificationPipeBuilder<PublishContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var typeBuilder = new Builder(builder);

        _messagePublishTopology.Apply(typeBuilder);
    }

    /// <summary>Returns no failures because this adapter has no independent configuration.</summary>
    /// <returns>An empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }


    sealed class Builder :
        ITopologyPipeBuilder<PublishContext<TMessage>>
    {
        readonly ISpecificationPipeBuilder<PublishContext<TMessage>> _builder;

        public Builder(ISpecificationPipeBuilder<PublishContext<TMessage>> builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void AddFilter(IFilter<PublishContext<TMessage>> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            _builder.AddFilter(filter);
        }

        public bool IsDelegated => _builder.IsDelegated;
        public bool IsImplemented => _builder.IsImplemented;

        public ITopologyPipeBuilder<PublishContext<TMessage>> CreateDelegatedBuilder()
        {
            return new ChildBuilder<PublishContext<TMessage>>(this, IsImplemented, true);
        }


        sealed class ChildBuilder<T> :
            ITopologyPipeBuilder<T>
            where T : class, PipeContext
        {
            readonly ITopologyPipeBuilder<T> _builder;

            public ChildBuilder(ITopologyPipeBuilder<T> builder, bool isImplemented, bool isDelegated)
            {
                _builder = builder ?? throw new ArgumentNullException(nameof(builder));

                IsDelegated = isDelegated;
                IsImplemented = isImplemented;
            }

            public void AddFilter(IFilter<T> filter)
            {
                ArgumentNullException.ThrowIfNull(filter);

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
