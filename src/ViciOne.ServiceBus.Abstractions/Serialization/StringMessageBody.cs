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

        /// <summary>
        /// The length of what is transmitted, which is UTF-8 bytes. The character count understates
        /// every body carrying a character outside ASCII, because those cost more than one byte each.
        /// </summary>
        public long? Length => _body != null ? Encoding.UTF8.GetByteCount(_body) : null;

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
