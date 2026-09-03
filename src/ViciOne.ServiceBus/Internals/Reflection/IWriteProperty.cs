namespace ViciOne.ServiceBus.Internals
{
    using System;


    internal interface IWriteProperty<in T, in TProperty> :
        IWriteProperty<T>
        where T : class
    {
        void Set(T entity, TProperty value);
    }


    internal interface IWriteProperty<in T>
        where T : class
    {
        Type TargetType { get; }
    }
}
