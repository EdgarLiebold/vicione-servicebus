using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Prevents broker topology from being created for the annotated message contract when it is published as an inherited type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class ExcludeFromTopologyAttribute :
    Attribute
{
}
