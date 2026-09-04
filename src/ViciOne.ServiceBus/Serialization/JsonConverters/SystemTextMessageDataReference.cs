using System;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.MessageData;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>
/// Provides a system text message data reference implementation.
/// </summary>
public class SystemTextMessageDataReference :
    IMessageDataReference
{
    /// <summary>
    /// Gets or sets the reference value.
    /// </summary>
    [JsonPropertyName("data-ref")]
    public Uri? Reference { get; set; }

    /// <summary>
    /// Gets or sets the text value.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the data value.
    /// </summary>
    [JsonPropertyName("data")]
    public byte[]? Data { get; set; }
}
