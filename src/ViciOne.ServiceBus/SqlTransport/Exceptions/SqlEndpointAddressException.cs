namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public sealed class SqlEndpointAddressException :
        AbstractUriException
    {
        public SqlEndpointAddressException()
        {
        }

        public SqlEndpointAddressException(Uri address, string message)
            : base(address, message)
        {
        }

        public SqlEndpointAddressException(Uri address, string message, Exception innerException)
            : base(address, message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        public SqlEndpointAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
