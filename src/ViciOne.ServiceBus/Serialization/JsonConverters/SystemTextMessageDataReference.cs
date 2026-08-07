// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization.JsonConverters
{
    using System;
    using System.Text.Json.Serialization;
    using MessageData;


    public class SystemTextMessageDataReference :
        IMessageDataReference
    {
        [JsonPropertyName("data-ref")]
        public Uri Reference { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("data")]
        public byte[] Data { get; set; }
    }
}
