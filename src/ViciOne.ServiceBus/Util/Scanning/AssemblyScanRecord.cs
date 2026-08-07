// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Util.Scanning
{
    using System;


    public class AssemblyScanRecord
    {
        public Exception LoadException;
        public string Name;

        public override string ToString()
        {
            return LoadException == null ? Name : $"{Name} (Failed)";
        }
    }
}
