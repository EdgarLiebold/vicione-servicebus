// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization.JsonConverters
{
    using System;
    using MessageData;
    using Newtonsoft.Json;


    public class MessageDataReference :
        IMessageDataReference
    {
        [JsonProperty("data-ref")]
        public Uri Reference { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("data")]
        public byte[] Data { get; set; }
    }
}
