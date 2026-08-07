// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.HangfireIntegration
{
    using Context;
    using Scheduling;


    public class HashedHangfireScheduledMessageData :
        HangfireScheduledMessageData,
        IHashedScheduleId
    {
        public string? HashId { get; set; }

        public static HashedHangfireScheduledMessageData Create(ConsumeContext<ScheduleMessage> context, string hashId)
        {
            var message = new HashedHangfireScheduledMessageData { HashId = hashId };

            var messageBody = context.SerializerContext.GetMessageSerializer(context.Message.Payload, context.Message.PayloadType)
                .GetMessageBody(new MessageSendContext<ScheduleMessage>(context.Message));

            SetBaseProperties(message, context, context.Message.Destination, messageBody, context.Message.PayloadType, context.Message.CorrelationId);

            return message;
        }
    }
}
