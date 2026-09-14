using System;

namespace ViciOne.ServiceBus.Internals.Reflection;

internal interface IWriteProperty<in T, in TProperty> :
    IWriteProperty<T>
    where T : class
{
    void Set(T entity, TProperty? value);
}


internal interface IWriteProperty<in T>
    where T : class
{
    Type TargetType { get; }
}
