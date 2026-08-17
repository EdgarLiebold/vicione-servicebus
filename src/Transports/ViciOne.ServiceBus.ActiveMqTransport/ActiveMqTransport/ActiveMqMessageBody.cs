namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using System.IO;
    using System.Text;
    using Apache.NMS;


    public class ActiveMqMessageBody :
        MessageBody
    {
        readonly IMessage _message;
        byte[] _bytes;

        public ActiveMqMessageBody(IMessage message)
        {
            _message = message;
        }

        /// <summary>
        /// The number of bytes this body transmits, which is by definition the length of what
        /// <see cref="GetBytes" /> returns, whichever accessor ran first. Reporting only the cached
        /// array meant the length was nothing at all until somebody had already read the body, so the
        /// same message answered differently depending on the order of two independent calls.
        /// <para>
        /// Answering it from the message a second time instead of from <see cref="GetBytes" /> would
        /// let the two drift apart and would touch a provider property that is not always readable, so
        /// it delegates: whatever the body is, and whatever refuses it, both members agree.
        /// </para>
        /// </summary>
        public long? Length => GetBytes().LongLength;

        public Stream GetStream()
        {
            return new MemoryStream(GetBytes(), false);
        }

        public byte[] GetBytes()
        {
            return _bytes ??= _message switch
            {
                ITextMessage text => Encoding.UTF8.GetBytes(text.Text),
                IBytesMessage bytes => bytes.Content,
                _ => throw new ActiveMqTransportException($"The message type is not supported: {TypeCache.GetShortName(_message.GetType())}")
            };
        }

        public string GetString()
        {
            return _message switch
            {
                ITextMessage text => text.Text,
                IBytesMessage bytes => MessageDefaults.Encoding.GetString(bytes.Content),
                _ => throw new ActiveMqTransportException($"The message type is not supported: {TypeCache.GetShortName(_message.GetType())}")
            };
        }
    }
}
