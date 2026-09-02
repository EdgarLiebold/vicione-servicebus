#nullable enable
namespace ViciOne.ServiceBus.Configuration
{
    using System.Text.Json;


    internal interface IJsonSerializerFactory
    {
        ISerializerFactory Bind(JsonSerializerOptions options);
    }
}
