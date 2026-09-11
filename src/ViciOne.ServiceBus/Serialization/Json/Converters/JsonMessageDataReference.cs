using System;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.MessageData;

namespace ViciOne.ServiceBus.Serialization.Json.Converters;

/// <summary>Represents the JSON wire shape of inline or externally stored message data.</summary>
internal sealed class JsonMessageDataReference :
    IMessageDataReference
{
    /// <summary>Gets or sets the external data address.</summary>
    [JsonPropertyName("data-ref")]
    public Uri? Reference { get; set; }

    /// <summary>Gets or sets inline text data.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Gets or sets inline binary data.</summary>
    [JsonPropertyName("data")]
    public byte[]? Data { get; set; }
}
