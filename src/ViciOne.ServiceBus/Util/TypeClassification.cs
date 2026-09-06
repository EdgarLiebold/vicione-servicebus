using System;

namespace ViciOne.ServiceBus.Util;

/// <summary>Specifies the available type classification values.</summary>
[Flags]
public enum TypeClassification :
    short
{
    /// <summary>Indicates all.</summary>
    All = 0,
    /// <summary>Indicates open.</summary>
    Open = 1,
    /// <summary>Indicates closed.</summary>
    Closed = 2,
    /// <summary>Indicates interface.</summary>
    Interface = 4,
    /// <summary>Indicates abstract.</summary>
    Abstract = 8,
    /// <summary>Indicates concrete.</summary>
    Concrete = 16
}
