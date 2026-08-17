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
        /// Read-only, like every other message body: writing back through the stream a caller is handed
        /// cannot change what everybody else reads. That closes one route, not all of them — the
        /// constructor takes a caller's array and <see cref="GetBytes" /> hands it straight back, so
        /// this body is not immutable and is not claimed to be.
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
