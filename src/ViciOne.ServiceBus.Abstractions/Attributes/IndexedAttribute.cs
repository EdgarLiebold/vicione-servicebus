// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// Specifies a property that should be indexed by the in-memory saga repository
    /// </summary>
    public class IndexedAttribute :
        Attribute
    {
    }
}
