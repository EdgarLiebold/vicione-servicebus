#nullable enable
namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using System.IO;
    using System.Text;
    using System.Threading;
    using Apache.NMS;


    public class ActiveMqMessageBody :
        MessageBody
    {
        readonly IMessage _message;
        readonly object _gate = new();
        byte[]? _bytes;
        bool _initialized;
        string? _string;

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
        public long? Length
        {
            get
            {
                EnsureInitialized();
                return _bytes!.LongLength;
            }
        }

        public Stream GetStream()
        {
            EnsureInitialized();
            return new MemoryStream(_bytes!, false);
        }

        public byte[] GetBytes()
        {
            EnsureInitialized();
            return _bytes!;
        }

        public string GetString()
        {
            EnsureInitialized();
            return _string!;
        }

        void EnsureInitialized()
        {
            if (Volatile.Read(ref _initialized))
                return;

            lock (_gate)
            {
                if (_initialized)
                    return;

                switch (_message)
                {
                    case ITextMessage text:
                        _string = text.Text ?? string.Empty;
                        _bytes = Encoding.UTF8.GetBytes(_string);
                        break;

                    case IBytesMessage bytes:
                        byte[]? content = bytes.Content;
                        _bytes = content is null ? [] : (byte[])content.Clone();
                        _string = MessageDefaults.Encoding.GetString(_bytes);
                        break;

                    default:
                        throw new ActiveMqTransportException(
                            $"The message type is not supported: {TypeCache.GetShortName(_message.GetType())}");
                }

                Volatile.Write(ref _initialized, true);
            }
        }
    }
}
