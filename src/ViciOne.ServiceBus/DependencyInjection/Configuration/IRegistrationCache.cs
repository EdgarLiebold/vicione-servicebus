namespace ViciOne.ServiceBus.Configuration
{
    using System.Collections.Generic;


    public interface IRegistrationCache<out T>
    {
        IEnumerable<T> Values { get; }
    }
}
