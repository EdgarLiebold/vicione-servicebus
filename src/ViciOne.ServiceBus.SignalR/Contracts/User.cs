// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR.Contracts
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.SignalR;


    public interface User<THub>
        where THub : Hub
    {
        string UserId { get; }
        IReadOnlyDictionary<string, byte[]> Messages { get; }
    }
}
