namespace ViciOne.ServiceBus
{
    using System;


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
    }
}
