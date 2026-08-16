namespace ViciOne.ServiceBus
{
    using System;


    public interface ITimeoutConfigurator
    {
        TimeSpan Timeout { set; }
    }
}
