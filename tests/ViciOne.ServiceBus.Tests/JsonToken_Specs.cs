// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests
{
    using System.IO;
    using ViciOne.ServiceBus.Serialization;
    using Newtonsoft.Json;
    using NUnit.Framework;


    [TestFixture]
    public class Using_a_JsonToken_to_convert_types
    {
        [Test]
        public void Should_support_int()
        {
            using (var reader = new StringReader("27"))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var value = NewtonsoftJsonMessageSerializer.Deserializer.Deserialize<int>(jsonReader);

                Assert.That(value, Is.EqualTo(27));
            }
        }
    }
}
