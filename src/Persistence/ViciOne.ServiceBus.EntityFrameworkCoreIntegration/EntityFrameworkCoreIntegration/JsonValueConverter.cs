using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

public class JsonValueConverter<T> :
    ValueConverter<T, string>
    where T : class?
{
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
