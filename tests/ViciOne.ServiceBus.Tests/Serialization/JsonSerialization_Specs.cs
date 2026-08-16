namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ViciOne.ServiceBus.Serialization;
    using Metadata;
    using NUnit.Framework;


    public class When_serializing_decimals
    {
        /// <summary>
        /// A decimal at its maximum has more digits than a double can hold, so a reader that widens it
        /// loses the value. The body is written out literally rather than produced by a second
        /// serializer: what this asserts is that the kept deserializer reads such a number exactly,
        /// whoever wrote it. It used to be written by Json.NET, which made the case look like a
        /// statement about that library instead of about the reader.
        /// </summary>
        [Test]
        public void Should_read_a_maximum_decimal_written_by_any_producer()
        {
            const string body = """{"Decimal":79228162514264337593543950335}""";

            var deserializedMessage = System.Text.Json.JsonSerializer.Deserialize<MessageA>(
                body, SystemTextJsonMessageSerializer.Options);

            Assert.That(deserializedMessage, Is.Not.Null);
            Assert.That(deserializedMessage.Decimal, Is.EqualTo(decimal.MaxValue));
        }


        class MessageA
        {
            public decimal Decimal { get; set; }
        }
    }
}
