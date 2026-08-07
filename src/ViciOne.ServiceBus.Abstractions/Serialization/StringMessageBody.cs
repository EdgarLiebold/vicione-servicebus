// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.IO;
    using System.Text;


    public class StringMessageBody :
        MessageBody
    {
        readonly string _body;
        byte[]? _bytes;

        public StringMessageBody(string body)
        {
            _body = body;
        }

        public long? Length => _body?.Length;

        public Stream GetStream()
        {
            return new MemoryStream(GetBytes());
        }

        public byte[] GetBytes()
        {
            return _bytes ??= string.IsNullOrWhiteSpace(_body)
                ? []
                : Encoding.UTF8.GetBytes(_body);
        }

        public string GetString()
        {
            return _body;
        }
    }
}
