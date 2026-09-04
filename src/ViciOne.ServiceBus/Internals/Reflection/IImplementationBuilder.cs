using System;

namespace ViciOne.ServiceBus.Internals;

internal interface IImplementationBuilder
{
    Type GetImplementationType(Type interfaceType);
}
