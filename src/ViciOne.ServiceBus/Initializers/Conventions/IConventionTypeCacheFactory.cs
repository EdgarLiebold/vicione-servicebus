// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers.Conventions
{
    public interface IConventionTypeCacheFactory<out TValue>
        where TValue : class
    {
        TValue Create<T>(IInitializerConvention convention)
            where T : class;
    }
}
