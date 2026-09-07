using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Prevents send and publish pipeline specifications from being created for the annotated implemented message contract.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class ExcludeFromImplementedTypesAttribute :
    Attribute
{
}
