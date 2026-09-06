using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ViciOne.ServiceBus.Serialization.JsonConverters;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Creates isolated System.Text.Json option graphs for message serialization.
/// </summary>
public static class SystemTextJsonSerializerOptions
{
    /// <summary>
    /// Creates default.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
                ? JsonTypeInfoResolver.Combine(SystemTextJsonSerializationContext.Default, new DefaultJsonTypeInfoResolver())
                : SystemTextJsonSerializationContext.Default
        };

        options.Converters.Add(new StringDecimalJsonConverter());
        options.Converters.Add(new SystemTextJsonMessageDataConverter());
        options.Converters.Add(new SystemTextJsonConverterFactory());

        return options;
    }

    internal static JsonSerializerOptions Freeze(JsonSerializerOptions options)
    {
        if (options == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", "System.Text.Json serializer options cannot be null.", "Correct the named configuration before starting the host"));

        var snapshot = new JsonSerializerOptions(options);
        snapshot.MakeReadOnly();
        return snapshot;
    }
}
