#nullable enable
namespace ViciOne.ServiceBus
{
    using System;


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
    }
}
