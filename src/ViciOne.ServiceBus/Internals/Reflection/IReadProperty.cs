namespace ViciOne.ServiceBus.Internals;

internal interface IReadProperty<in T, out TProperty> :
    IReadProperty<T>
    where T : class
{
    TProperty Get(T entity);
}


internal interface IReadProperty<in T>
{
}
