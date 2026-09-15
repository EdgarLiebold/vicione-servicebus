using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies the consume topology for a message contract to a consume pipe.</summary>
/// <typeparam name="TMessage">The consumed message contract type.</typeparam>
public sealed class MessageConsumeTopologyPipeSpecification<TMessage> :
    ISpecificationPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly IMessageConsumeTopology<TMessage> _messageConsumeTopology;

    /// <summary>Initializes the specification with the topology to apply.</summary>
    /// <param name="messageConsumeTopology">The consume-message topology.</param>
    public MessageConsumeTopologyPipeSpecification(IMessageConsumeTopology<TMessage> messageConsumeTopology)
    {
        _messageConsumeTopology = messageConsumeTopology ?? throw new ArgumentNullException(nameof(messageConsumeTopology));
    }

    /// <summary>Applies the configured message topology to a consume-pipe builder.</summary>
    /// <param name="builder">The consume-pipe builder.</param>
    public void Apply(ISpecificationPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var typeBuilder = new Builder(builder);

        _messageConsumeTopology.Apply(typeBuilder);
    }

    /// <summary>Returns no failures because this adapter has no independent configuration.</summary>
    /// <returns>An empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }


    sealed class Builder :
        ITopologyPipeBuilder<ConsumeContext<TMessage>>
    {
        readonly ISpecificationPipeBuilder<ConsumeContext<TMessage>> _builder;

        public Builder(ISpecificationPipeBuilder<ConsumeContext<TMessage>> builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void AddFilter(IFilter<ConsumeContext<TMessage>> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            _builder.AddFilter(filter);
        }

        public bool IsDelegated => _builder.IsDelegated;
        public bool IsImplemented => _builder.IsImplemented;

        public ITopologyPipeBuilder<ConsumeContext<TMessage>> CreateDelegatedBuilder()
        {
            return new ChildBuilder<ConsumeContext<TMessage>>(this, IsImplemented, true);
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
