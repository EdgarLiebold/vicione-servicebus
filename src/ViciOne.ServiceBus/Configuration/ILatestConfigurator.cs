// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface ILatestConfigurator<T>
        where T : class, PipeContext
    {
        LatestFilterCreated<T> Created { set; }
    }
}
