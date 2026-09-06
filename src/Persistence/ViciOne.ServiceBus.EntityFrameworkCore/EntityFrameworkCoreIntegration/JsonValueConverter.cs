using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Converts a reference-type property to and from JSON for relational persistence.</summary>
/// <typeparam name="T">The reference type converted to JSON.</typeparam>
public class JsonValueConverter<T> :
    ValueConverter<T, string>
    where T : class?
{
    /// <summary>Initializes the converter with optional provider mapping hints.</summary>
    /// <param name="hints">Mapping hints forwarded to EF Core.</param>
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
