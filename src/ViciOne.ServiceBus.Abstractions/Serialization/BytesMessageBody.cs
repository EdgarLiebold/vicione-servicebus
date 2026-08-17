namespace ViciOne.ServiceBus
{
    using System.IO;
    using System.Text;


    public class BytesMessageBody :
        MessageBody
    {
        readonly byte[] _bytes;
        string? _string;

        public BytesMessageBody(byte[]? bytes)
        {
            _bytes = bytes ?? [];
        }

        public long? Length => _bytes.Length;

        /// <summary>
        /// Read-only, like every other message body: the body is immutable, so a caller may not write
        /// back through the stream it is handed and change what everyone else reads.
        /// </summary>
        public Stream GetStream()
        {
            return new MemoryStream(_bytes, false);
        }

        public byte[] GetBytes()
        {
            return _bytes;
        }

        public string GetString()
        {
            return _string ??= Encoding.UTF8.GetString(_bytes);
        }
    }
}
