using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.Json.Converters;

/// <summary>Uses a dedicated option graph for one configured message contract.</summary>
/// <typeparam name="T">The configured message contract.</typeparam>
internal sealed class CustomMessageTypeJsonConverter<T> :
    JsonConverter<T>
    where T : class
{
    readonly JsonSerializerOptions _options;

    /// <summary>Creates a converter bound to the supplied serializer options.</summary>
    /// <param name="options">The option graph dedicated to <typeparamref name="T" />.</param>
    public CustomMessageTypeJsonConverter(JsonSerializerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Deserializes the configured contract with its dedicated options.</summary>
    /// <param name="reader">The reader positioned at the message value.</param>
    /// <param name="typeToConvert">The configured message contract.</param>
    /// <param name="options">The surrounding serializer options; the dedicated options are used instead.</param>
    /// <returns>The deserialized contract value.</returns>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, _options);
    }

    /// <summary>Serializes the configured contract with its dedicated options.</summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The configured message value.</param>
    /// <param name="options">The surrounding serializer options; the dedicated options are used instead.</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, _options);
    }
}
