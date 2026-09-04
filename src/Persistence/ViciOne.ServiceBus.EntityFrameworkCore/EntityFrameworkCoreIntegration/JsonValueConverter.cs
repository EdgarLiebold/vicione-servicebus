using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides a json value converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class JsonValueConverter<T> :
    ValueConverter<T, string>
    where T : class?
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hints">The hints value.</param>
    public JsonValueConverter(ConverterMappingHints? hints = default)
        : base(v => Serialize(v), v => Deserialize(v), hints)
    {
    }

    static T Deserialize(string json)
    {
        return JsonSerializer.Deserialize<T>(json, ServiceBusMetadataJson.Options)!;
    }

    static string Serialize(T obj)
    {
        return JsonSerializer.Serialize(obj, ServiceBusMetadataJson.Options);
    }
}
