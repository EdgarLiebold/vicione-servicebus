using System;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Composes saga and message pipelines and connects them to the consume pipeline.</summary>
    public abstract class SagaMessageConnector :
        ISagaMessageConnector<TSaga>
    {
        const ConnectPipeOptions NotConfigureConsumeTopology = ConnectPipeOptions.All & ~ConnectPipeOptions.ConfigureConsumeTopology;
        readonly IFilter<SagaConsumeContext<TSaga, TMessage>> _consumeFilter;

        /// <summary>Associates the consume filter appended to the saga-specific pipeline.</summary>
        /// <param name="consumeFilter">The filter handling the saga/message consume context.</param>
        protected SagaMessageConnector(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter)
        {
            _consumeFilter = consumeFilter;
        }

        /// <summary>Gets whether connecting this message pipeline also configures consume topology.</summary>
        protected virtual bool ConfigureConsumeTopology { get; } = true;

        /// <summary>Gets the message contract connected by this connector.</summary>
        public Type MessageType => typeof(TMessage);

        /// <summary>Creates an empty specification for this saga/message pair.</summary>
        /// <returns>The specification configuring the saga-specific and message pipelines.</returns>
        public ISagaMessageSpecification<TSaga> CreateSagaMessageSpecification()
        {
            return new SagaMessageSpecification();
        }

        /// <summary>Builds the configured message and saga pipelines and connects the message pipeline for consumption.</summary>
        /// <param name="consumePipe">The connector receiving the completed message pipeline.</param>
        /// <param name="repository">The saga repository used by the message-dispatch filters.</param>
        /// <param name="specification">The saga configuration supplying message-specific and shared specifications.</param>
        /// <returns>A handle that disconnects the registration.</returns>
        public ConnectHandle ConnectSaga(IConsumePipeConnector consumePipe, ISagaRepository<TSaga> repository, ISagaSpecification<TSaga> specification)
        {
            ISagaMessageSpecification<TSaga, TMessage> messageSpecification = specification.GetMessageSpecification<TMessage>();

            IPipe<SagaConsumeContext<TSaga, TMessage>> consumerPipe = messageSpecification.BuildConsumerPipe(_consumeFilter);

            IPipe<ConsumeContext<TMessage>> messagePipe = messageSpecification.BuildMessagePipe(x =>
            {
                specification.ConfigureMessagePipe(x);

                ConfigureMessagePipe(x, repository, consumerPipe);
            });

            return ConfigureConsumeTopology
                ? consumePipe.ConnectConsumePipe(messagePipe)
                : consumePipe.ConnectConsumePipe(messagePipe, NotConfigureConsumeTopology);
        }

        /// <summary>Appends message-dispatch filters that enter the saga repository before the saga-specific pipeline.</summary>
        /// <param name="configurator">The message pipeline receiving the dispatch filters.</param>
        /// <param name="repository">The saga repository used by the dispatch filters.</param>
        /// <param name="sagaPipe">The pipeline invoked with the selected saga context.</param>
        protected abstract void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TMessage>> configurator, ISagaRepository<TSaga> repository,
            IPipe<SagaConsumeContext<TSaga, TMessage>> sagaPipe);
    }
}
