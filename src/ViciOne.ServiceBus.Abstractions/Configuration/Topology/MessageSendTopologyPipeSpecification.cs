using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies the send topology for a message contract to a send pipe.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
public sealed class MessageSendTopologyPipeSpecification<TMessage> :
    ISpecificationPipeSpecification<SendContext<TMessage>>
    where TMessage : class
{
    readonly IMessageSendTopology<TMessage> _messageSendTopology;

    /// <summary>Initializes the specification with the topology to apply.</summary>
    /// <param name="messageSendTopology">The send-message topology.</param>
    public MessageSendTopologyPipeSpecification(IMessageSendTopology<TMessage> messageSendTopology)
    {
        _messageSendTopology = messageSendTopology ?? throw new ArgumentNullException(nameof(messageSendTopology));
    }

    /// <summary>Applies the configured message topology to a send-pipe builder.</summary>
    /// <param name="builder">The send-pipe builder.</param>
    public void Apply(ISpecificationPipeBuilder<SendContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var typeBuilder = new Builder(builder);

        _messageSendTopology.Apply(typeBuilder);
    }

    /// <summary>Returns no failures because this adapter has no independent configuration.</summary>
    /// <returns>An empty sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }


    sealed class Builder :
        ITopologyPipeBuilder<SendContext<TMessage>>
    {
        readonly ISpecificationPipeBuilder<SendContext<TMessage>> _builder;

        public Builder(ISpecificationPipeBuilder<SendContext<TMessage>> builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void AddFilter(IFilter<SendContext<TMessage>> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            _builder.AddFilter(filter);
        }

        public bool IsDelegated => _builder.IsDelegated;
        public bool IsImplemented => _builder.IsImplemented;

        public ITopologyPipeBuilder<SendContext<TMessage>> CreateDelegatedBuilder()
        {
            return new ChildBuilder<SendContext<TMessage>>(this, IsImplemented, true);
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
