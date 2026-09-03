namespace ViciOne.ServiceBus.Internals
{
    using System.Reflection;


    internal interface IWritePropertyCache<in T>
        where T : class
    {
        bool CanWrite(string name);

        IWriteProperty<T, TProperty> GetProperty<TProperty>(string name);
        IWriteProperty<T, TProperty> GetProperty<TProperty>(PropertyInfo propertyInfo);
    }
}
