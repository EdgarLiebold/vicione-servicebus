// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class MessageDataNotFoundException :
        MessageDataException
    {
        public MessageDataNotFoundException()
        {
        }

        public MessageDataNotFoundException(Uri address)
            : base($"The message data was not found: {address}")
        {
        }
    }
}
