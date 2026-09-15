using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Projects send topology configuration into matching publish topologies.</summary>
public sealed class PublishToSendTopologyConfigurationObserver :
    IPublishTopologyConfigurationObserver
{
    readonly ISendTopology _sendTopology;

    /// <summary>Initializes the observer with the send topology to project.</summary>
    /// <param name="sendTopology">The source send topology.</param>
    public PublishToSendTopologyConfigurationObserver(ISendTopology sendTopology)
    {
        _sendTopology = sendTopology ?? throw new ArgumentNullException(nameof(sendTopology));
    }

    /// <summary>Adds the matching send-message topology to a created publish-message topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The publish-message topology configurator receiving the projection.</param>
    public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);

        IMessageSendTopology<T> messageSendTopology = _sendTopology.GetMessageTopology<T>();

        configurator.AddDelegate(new Proxy<T>(messageSendTopology));
    }


    sealed class Proxy<TMessage> :
        IMessagePublishTopology<TMessage>
        where TMessage : class
    {
        readonly IMessageSendTopology<TMessage> _topology;

        public Proxy(IMessageSendTopology<TMessage> topology)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
        }

        public void Apply(ITopologyPipeBuilder<PublishContext<TMessage>> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            var sendBuilder = new Builder(builder);

            _topology.Apply(sendBuilder);
        }

        public bool Exclude => false;

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            ArgumentNullException.ThrowIfNull(baseAddress);

            publishAddress = null;
            return false;
        }


        sealed class Builder :
            ITopologyPipeBuilder<SendContext<TMessage>>
        {
            readonly ITopologyPipeBuilder<PublishContext<TMessage>> _builder;

            public Builder(ITopologyPipeBuilder<PublishContext<TMessage>> builder)
            {
                _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            }

            public void AddFilter(IFilter<SendContext<TMessage>> filter)
            {
                ArgumentNullException.ThrowIfNull(filter);

                var splitFilter = new SplitFilter<PublishContext<TMessage>, SendContext<TMessage>>(filter, MergeContext, FilterContext);

                _builder.AddFilter(splitFilter);
            }

            public bool IsDelegated => _builder.IsDelegated;
            public bool IsImplemented => _builder.IsImplemented;

            public ITopologyPipeBuilder<SendContext<TMessage>> CreateDelegatedBuilder()
            {
                return new ChildBuilder<SendContext<TMessage>>(this, IsImplemented, true);
            }

            static SendContext<TMessage> FilterContext(PublishContext<TMessage> context)
            {
                return context;
            }

            static PublishContext<TMessage> MergeContext(PublishContext<TMessage> input, SendContext context)
            {
                return context.GetPayload<PublishContext<TMessage>>();
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
}
