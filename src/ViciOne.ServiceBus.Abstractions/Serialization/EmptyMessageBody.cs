// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.IO;


    public class EmptyMessageBody :
        MessageBody
    {
        public static MessageBody Instance { get; } = new EmptyMessageBody();

        public long? Length => 0;

        public Stream GetStream()
        {
            return new MemoryStream();
        }

        public byte[] GetBytes()
        {
            return [];
        }

        public string GetString()
        {
            return string.Empty;
        }
    }
}
