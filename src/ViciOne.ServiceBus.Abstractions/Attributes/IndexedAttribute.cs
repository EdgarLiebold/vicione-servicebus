using System;

namespace ViciOne.ServiceBus;

/// <summary>Specifies a property that should be indexed by the in-memory saga repository.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class IndexedAttribute :
    Attribute
{
}
