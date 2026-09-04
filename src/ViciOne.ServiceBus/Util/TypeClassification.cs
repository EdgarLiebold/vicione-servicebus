using System;

namespace ViciOne.ServiceBus.Util;

[Flags]
public enum TypeClassification :
    short
{
    All = 0,
    Open = 1,
    Closed = 2,
    Interface = 4,
    Abstract = 8,
    Concrete = 16
}
