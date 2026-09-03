namespace ViciOne.ServiceBus.Internals
{
    using System;


    internal interface IImplementationBuilder
    {
        Type GetImplementationType(Type interfaceType);
    }
}
