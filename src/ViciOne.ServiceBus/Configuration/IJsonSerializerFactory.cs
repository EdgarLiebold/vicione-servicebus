using System.Text.Json;

namespace ViciOne.ServiceBus.Configuration;

internal interface IJsonSerializerFactory
{
    ISerializerFactory Bind(JsonSerializerOptions options);
}
