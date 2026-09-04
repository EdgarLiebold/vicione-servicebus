using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

public interface IRegistrationCache<out T>
{
    IEnumerable<T> Values { get; }
}
