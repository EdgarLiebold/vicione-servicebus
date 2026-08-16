// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests
{
    using System.Collections.Generic;
    using System.Text.Json;
    using ViciOne.ServiceBus.Serialization;
    using NUnit.Framework;


    /// <summary>
    /// The product converts a parsed JSON element into a requested type through GetObject and Transform. Until now
    /// that surface had no test of its own: the only case covering the same idea drove the serializer that leaves,
    /// so removing it would have left the kept path unexercised without anyone noticing.
    ///
    /// The removed case converted a bare scalar. This surface is declared for reference types, so a scalar is
    /// not part of what the product offers here and is not restated as if it were.
    /// </summary>
    [TestFixture]
    public class Using_a_json_element_to_convert_types
    {
        [Test]
        public void Should_support_an_object()
        {
            using JsonDocument document = JsonDocument.Parse("{\"name\":\"bob\",\"count\":3}");

            var value = document.RootElement.GetObject<Counted>(SystemTextJsonMessageSerializer.Options);

            Assert.Multiple(() =>
            {
                Assert.That(value.Name, Is.EqualTo("bob"));
                Assert.That(value.Count, Is.EqualTo(3));
            });
        }

        [Test]
        public void Should_transform_an_object_into_another_shape()
        {
            var source = new Dictionary<string, object> { { "name", "bob" }, { "count", 3 } };

            var value = source.Transform<Counted>(SystemTextJsonMessageSerializer.Options);

            Assert.Multiple(() =>
            {
                Assert.That(value.Name, Is.EqualTo("bob"));
                Assert.That(value.Count, Is.EqualTo(3));
            });
        }


        public class Counted
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }
    }
}
