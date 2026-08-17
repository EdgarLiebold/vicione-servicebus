namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class CommandException :
        ViciOneServiceBusException
    {
        public CommandException()
        {
        }

        public CommandException(string message)
            : base(message)
        {
        }

        public CommandException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
