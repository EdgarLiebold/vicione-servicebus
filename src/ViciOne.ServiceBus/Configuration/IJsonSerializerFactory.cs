using System.Text.Json;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

internal interface IJsonSerializerFactory
{
    ISerializerFactory Bind(JsonSerializerOptions options);
}
