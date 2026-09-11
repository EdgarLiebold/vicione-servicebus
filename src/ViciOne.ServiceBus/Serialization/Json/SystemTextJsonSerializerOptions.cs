using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ViciOne.ServiceBus.Serialization.Json.Converters;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Creates isolated System.Text.Json option graphs for message serialization.</summary>
public static class SystemTextJsonSerializerOptions
{
    /// <summary>Creates a mutable option graph with the ServiceBus JSON wire conventions.</summary>
    /// <returns>A new independently configurable option graph.</returns>
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

    /// <summary>Creates an immutable snapshot that can be shared safely by serializer instances.</summary>
    /// <param name="options">The option graph to copy.</param>
    /// <returns>An immutable copy of <paramref name="options" />.</returns>
    internal static JsonSerializerOptions Freeze(JsonSerializerOptions options)
    {
        if (options == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Serialization", "unknown", "System.Text.Json serializer options cannot be null.", "Correct the named configuration before starting the host"));

        var snapshot = new JsonSerializerOptions(options);
        snapshot.MakeReadOnly();
        return snapshot;
    }
}
