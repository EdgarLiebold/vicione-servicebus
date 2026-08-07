// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IMessageSendPipeSpecification<TMessage> :
        IPipeConfigurator<SendContext<TMessage>>,
        ISpecificationPipeSpecification<SendContext<TMessage>>
        where TMessage : class
    {
        void AddParentMessageSpecification(ISpecificationPipeSpecification<SendContext<TMessage>> parentSpecification);

        /// <summary>
        /// Build the pipe for the specification
        /// </summary>
        /// <returns></returns>
        IPipe<SendContext<TMessage>> BuildMessagePipe();
    }


    public interface IMessageSendPipeSpecification :
        IPipeConfigurator<SendContext>,
        ISpecification
    {
        IMessageSendPipeSpecification<T> GetMessageSpecification<T>()
            where T : class;
    }
}
