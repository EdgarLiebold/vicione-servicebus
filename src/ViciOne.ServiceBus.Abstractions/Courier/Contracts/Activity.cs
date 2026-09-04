using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

public interface Activity
{
    string Name { get; }

    Uri Address { get; }

    IDictionary<string, object> Arguments { get; }
}
