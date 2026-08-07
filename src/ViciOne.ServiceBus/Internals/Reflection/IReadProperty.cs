// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals
{
    public interface IReadProperty<in T, out TProperty> :
        IReadProperty<T>
        where T : class
    {
        TProperty Get(T entity);
    }


    public interface IReadProperty<in T>
    {
    }
}
