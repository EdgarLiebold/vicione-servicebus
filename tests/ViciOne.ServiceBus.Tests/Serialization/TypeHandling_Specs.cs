// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System.Text.Json;
    using ViciOne.ServiceBus.Serialization;
    using NUnit.Framework;


    /// <summary>
    /// A message body must never decide which type is loaded. A body that carries a type marker is asking the
    /// deserializer to resolve a name the sender chose, which is how a payload turns into code execution. The
    /// marker is data like any other property and the requested contract decides the type.
    ///
    /// The case used to state this only by not throwing, and it asserted nothing at all. It now names both halves.
    /// </summary>
    [TestFixture]
    public class Should_ignore_the_type_attribute
    {
        const string HostileBody =
            "{\"$type\":\"Command.TestCommand, TestDeserializationWithDummyClasses\",\"Id\":1,\"Name\":\"bob\"}";

        [Test]
        public void When_deserializing_a_json_body_with_types()
        {
            var command = JsonSerializer.Deserialize<ITestCommand>(HostileBody, SystemTextJsonMessageSerializer.Options);

            Assert.That(command, Is.Not.Null, "The body must still deserialize into the requested contract");

            Assert.Multiple(() =>
            {
                Assert.That(command.Id, Is.EqualTo(1), "The payload values must survive the ignored type marker");
                Assert.That(command.Name, Is.EqualTo("bob"));
                Assert.That(command.GetType().FullName, Does.Not.Contain("TestDeserializationWithDummyClasses"),
                    "The type named in the body must not decide which type is created");
            });
        }


        public interface ITestCommand
        {
            int Id { get; }
            string Name { get; }
        }


        public class TestCommand : ITestCommand
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
