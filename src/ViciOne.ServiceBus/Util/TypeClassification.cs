// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Util
{
    using System;


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
}
