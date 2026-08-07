// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization.JsonConverters
{
    using System;
    using System.Collections.Generic;


    public class CaseInsensitiveDictionary<T> :
        Dictionary<string, T>
    {
        public CaseInsensitiveDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
