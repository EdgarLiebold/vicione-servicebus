#nullable enable
namespace ViciOne.ServiceBus.Licensing
{
    using System.Collections.Generic;


    public class LicenseFile
    {
        public string? Version { get; set; }
        public string? Kind { get; set; }
        public Dictionary<string, object>? Meta { get; set; }
        public string? Data { get; set; }
        public string? Signature { get; set; }
    }
}
