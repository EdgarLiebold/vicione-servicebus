// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    public delegate TKey KeyAccessor<in TContext, out TKey>(TContext context);
}
