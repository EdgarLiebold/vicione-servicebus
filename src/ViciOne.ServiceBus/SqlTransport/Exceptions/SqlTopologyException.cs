#nullable enable
namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class SqlTopologyException :
        ViciOneServiceBusException
    {
        public SqlTopologyException()
        {
        }

        public SqlTopologyException(string? message)
            : base(message)
        {
        }

        public SqlTopologyException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected SqlTopologyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
