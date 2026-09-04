using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a saga connector implementation.
/// </summary>
public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>
    /// Provides a saga message connector implementation.
    /// </summary>
    public abstract class SagaMessageConnector :
        ISagaMessageConnector<TSaga>
    {
        const ConnectPipeOptions NotConfigureConsumeTopology = ConnectPipeOptions.All & ~ConnectPipeOptions.ConfigureConsumeTopology;
        readonly IFilter<SagaConsumeContext<TSaga, TMessage>> _consumeFilter;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="consumeFilter">The consume filter value.</param>
        protected SagaMessageConnector(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter)
        {
            _consumeFilter = consumeFilter;
        }

        /// <summary>
        /// Gets the configure consume topology value.
        /// </summary>
        protected virtual bool ConfigureConsumeTopology { get; } = true;

        /// <summary>
        /// Gets the message type value.
        /// </summary>
        public Type MessageType => typeof(TMessage);

        /// <summary>
        /// Creates saga message specification.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public ISagaMessageSpecification<TSaga> CreateSagaMessageSpecification()
        {
            return new SagaMessageSpecification();
        }

        /// <summary>
        /// Connects saga.
        /// </summary>
        /// <param name="consumePipe">The consume pipe value.</param>
        /// <param name="repository">The repository value.</param>
        /// <param name="specification">The specification value.</param>
        /// <returns>The result of the operation.</returns>
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

        /// <summary>
        /// Configure the message pipe that is prior to the saga repository
        /// </summary>
        /// <param name="configurator">The pipe configurator</param>
        /// <param name="repository"></param>
        /// <param name="sagaPipe"></param>
        protected abstract void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TMessage>> configurator, ISagaRepository<TSaga> repository,
            IPipe<SagaConsumeContext<TSaga, TMessage>> sagaPipe);
    }
}
