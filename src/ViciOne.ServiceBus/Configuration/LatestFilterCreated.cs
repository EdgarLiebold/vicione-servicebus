// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Middleware;


    public delegate void LatestFilterCreated<T>(ILatestFilter<T> filter)
        where T : class, PipeContext;
}
