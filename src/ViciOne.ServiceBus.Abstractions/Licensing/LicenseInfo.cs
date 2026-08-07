// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Licensing
{
    using System;
    using System.Collections.Generic;


    public class LicenseInfo
    {
        public LicenseContact? Contact { get; set; }
        public LicenseCustomer? Customer { get; set; }

        public Dictionary<string, LicenseProduct>? Products { get; set; }

        public DateTime Created { get; set; }
        public DateTime Expires { get; set; }
    }
}
