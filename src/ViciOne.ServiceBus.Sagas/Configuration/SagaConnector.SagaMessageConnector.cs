using System;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Connects saga message to the service bus pipeline.</summary>
    public abstract class SagaMessageConnector :
        ISagaMessageConnector<TSaga>
    {
        const ConnectPipeOptions NotConfigureConsumeTopology = ConnectPipeOptions.All & ~ConnectPipeOptions.ConfigureConsumeTopology;
        readonly IFilter<SagaConsumeContext<TSaga, TMessage>> _consumeFilter;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="consumeFilter">The consume filter.</param>
        protected SagaMessageConnector(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter)
        {
            _consumeFilter = consumeFilter;
        }

        /// <summary>Gets the configure consume topology.</summary>
        protected virtual bool ConfigureConsumeTopology { get; } = true;

        /// <summary>Gets the message type.</summary>
        public Type MessageType => typeof(TMessage);

        /// <summary>Creates saga message specification.</summary>
        /// <returns>The created saga message specification.</returns>
        public ISagaMessageSpecification<TSaga> CreateSagaMessageSpecification()
        {
            return new SagaMessageSpecification();
        }

        /// <summary>Connects saga.</summary>
        /// <param name="consumePipe">The consume pipe.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="specification">The specification.</param>
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

        /// <summary>Configure the message pipe that is prior to the saga repository.</summary>
        /// <param name="configurator">The pipe configurator.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="sagaPipe">The saga pipe.</param>
        protected abstract void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TMessage>> configurator, ISagaRepository<TSaga> repository,
            IPipe<SagaConsumeContext<TSaga, TMessage>> sagaPipe);
    }
}
