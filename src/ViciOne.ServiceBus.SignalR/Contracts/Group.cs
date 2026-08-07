// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR.Contracts
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.SignalR;


    public interface Group<THub>
        where THub : Hub
    {
        string GroupName { get; }
        string[] ExcludedConnectionIds { get; }
        IReadOnlyDictionary<string, byte[]> Messages { get; }
    }
}
