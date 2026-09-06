using System;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.MessageData;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>Carries a reference to system text message data.</summary>
public class SystemTextMessageDataReference :
    IMessageDataReference
{
    /// <summary>Gets or sets the reference.</summary>
    [JsonPropertyName("data-ref")]
    public Uri? Reference { get; set; }

    /// <summary>Gets or sets the text.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Gets or sets the data.</summary>
    [JsonPropertyName("data")]
    public byte[]? Data { get; set; }
}
