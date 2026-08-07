// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using StackExchange.Redis;


    public delegate IDatabase SelectDatabase(IConnectionMultiplexer multiplexer);
}
