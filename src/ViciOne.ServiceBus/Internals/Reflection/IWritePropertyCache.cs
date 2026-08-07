// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals
{
    using System.Reflection;


    public interface IWritePropertyCache<in T>
        where T : class
    {
        bool CanWrite(string name);

        IWriteProperty<T, TProperty> GetProperty<TProperty>(string name);
        IWriteProperty<T, TProperty> GetProperty<TProperty>(PropertyInfo propertyInfo);
    }
}
