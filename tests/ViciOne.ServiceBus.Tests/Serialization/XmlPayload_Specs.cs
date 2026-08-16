namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Text;
    using ViciOne.ServiceBus.Serialization;
    using NUnit.Framework;


    /// <summary>
    /// The product no longer carries an XML bus serializer, and it does not need one to carry XML. A document an
    /// application already holds as text or as bytes is ordinary payload, and the serializers the product keeps have
    /// to return it unchanged.
    ///
    /// This says nothing about a content type: the message travels as the JSON or MessagePack the bus speaks, and
    /// the XML is a value inside it, not the wire format.
    /// </summary>
    [TestFixture(typeof(SystemTextJsonMessageSerializer))]
    [TestFixture(typeof(MessagePackMessageSerializer))]
    public class Carrying_an_xml_document_as_payload :
        SerializationTest
    {
        const string Document =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
            + "<order id=\"4711\">"
            + "<customer name=\"Grüße &amp; Co\" />"
            + "<line sku=\"A-1\" qty=\"2\" />"
            + "<note><![CDATA[keep <this> verbatim]]></note>"
            + "</order>";

        [Test]
        public void Should_return_a_string_payload_unchanged()
        {
            var message = new Message { Text = Document };

            Message result = SerializeAndReturn(message);

            Assert.That(result.Text, Is.EqualTo(Document),
                "An XML document held as text must survive the roundtrip character for character");
        }

        [Test]
        public void Should_return_a_byte_payload_unchanged()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(Document);
            var message = new Message { Bytes = bytes };

            Message result = SerializeAndReturn(message);

            Assert.Multiple(() =>
            {
                Assert.That(result.Bytes, Is.EqualTo(bytes),
                    "An XML document held as bytes must survive the roundtrip byte for byte");
                Assert.That(Encoding.UTF8.GetString(result.Bytes), Is.EqualTo(Document));
            });
        }

        [Test]
        public void Should_not_advertise_an_xml_content_type()
        {
            // The bus speaks its own content type. Carrying XML as a value does not turn the message into XML.
            Assert.That(Serializer.ContentType.MediaType, Does.Not.Contain("xml"),
                "Carrying an XML payload must not present the message as an XML bus serialization");
        }


        public class Message
        {
            public string Text { get; set; }
            public byte[] Bytes { get; set; }
        }

        public Carrying_an_xml_document_as_payload(Type serializerType)
            : base(serializerType)
        {
        }
    }
}
