// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Licensing
{
    using System.Text.Json;


    public static class LicenseSettings
    {
        public static readonly JsonSerializerOptions SerializerOptions;

        static LicenseSettings()
        {
            SerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
    }
}
