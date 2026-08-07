// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Courier.Contracts
{
    using System;
    using System.Collections.Generic;


    public interface Activity
    {
        string Name { get; }

        Uri Address { get; }

        IDictionary<string, object> Arguments { get; }
    }
}
