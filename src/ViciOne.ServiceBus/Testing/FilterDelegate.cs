// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing
{
    public delegate bool FilterDelegate<in TContext>(TContext context)
        where TContext : class;
}
