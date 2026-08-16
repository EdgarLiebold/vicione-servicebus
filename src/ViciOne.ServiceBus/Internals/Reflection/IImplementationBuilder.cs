namespace ViciOne.ServiceBus.Internals
{
    using System;


    public interface IImplementationBuilder
    {
        Type GetImplementationType(Type interfaceType);
    }
}
