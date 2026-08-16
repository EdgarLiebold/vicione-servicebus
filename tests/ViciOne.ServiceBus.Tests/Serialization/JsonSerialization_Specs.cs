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
        /// The product writes a decimal as a quoted string, not as a JSON number: both the removed and
        /// the kept serializer register a converter that does so, because a decimal at its maximum has
        /// more digits than a reader that widens it to a double can keep.
        /// <para>
        /// So the body below is quoted and its property is camel case, which is exactly what the wire
        /// carries. An earlier version of this case wrote an unquoted number in Pascal case; it passed
        /// on case insensitivity and on the trivial number path, and would have stayed green even if
        /// the reader had lost the string form entirely.
        /// </para>
        /// </summary>
        [Test]
        public void Should_read_a_maximum_decimal_from_the_string_form_the_product_writes()
        {
            const string body = """{"decimal":"79228162514264337593543950335"}""";

            var deserializedMessage = System.Text.Json.JsonSerializer.Deserialize<MessageA>(
                body, SystemTextJsonMessageSerializer.Options);

            Assert.That(deserializedMessage, Is.Not.Null);
            Assert.That(deserializedMessage.Decimal, Is.EqualTo(decimal.MaxValue));
        }

        [Test]
        public void Should_write_a_decimal_in_the_string_form_a_reader_can_keep()
        {
            var body = System.Text.Json.JsonSerializer.Serialize(
                new MessageA { Decimal = decimal.MaxValue }, SystemTextJsonMessageSerializer.Options);

            Assert.That(body, Does.Contain("\"79228162514264337593543950335\""),
                "A bare JSON number would be widened by readers that have no decimal type");
        }

        class MessageA
        {
            public decimal Decimal { get; set; }
        }
    }
}
