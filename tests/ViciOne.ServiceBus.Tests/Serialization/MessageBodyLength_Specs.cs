namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System.Text;
    using System.Text.Json;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Serialization;


    /// <summary>
    /// A message body reports the length of what is transmitted, and what is transmitted is UTF-8.
    /// <para>
    /// The length was taken from whichever representation happened to exist, so a body that had been
    /// read as a string reported its character count. For anything outside ASCII that is smaller than
    /// the transmitted size, and the same body reported two different lengths depending on which
    /// accessor ran first.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Reporting_the_length_of_a_message_body
    {
        // Four characters, seven UTF-8 bytes: one ASCII, one two byte, one three byte, one one byte.
        const string NonAscii = "aäあb";

        [Test]
        public void Should_report_utf8_bytes_for_an_object_body_read_as_a_string_first()
        {
            var body = ObjectBody();

            var text = body.GetString();

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain(NonAscii));
                Assert.That(body.Length, Is.EqualTo(Encoding.UTF8.GetByteCount(text)));
                Assert.That(body.Length, Is.Not.EqualTo(text.Length),
                    "The character count and the transmitted size differ for this body, which is the point");
            });
        }

        [Test]
        public void Should_report_the_same_length_for_an_object_body_in_both_access_orders()
        {
            var readStringFirst = ObjectBody();
            readStringFirst.GetString();

            var readBytesFirst = ObjectBody();
            readBytesFirst.GetBytes();

            Assert.That(readStringFirst.Length, Is.EqualTo(readBytesFirst.Length));
        }

        [Test]
        public void Should_report_what_an_object_body_actually_carries()
        {
            var body = ObjectBody();
            body.GetString();

            Assert.That(body.Length, Is.EqualTo(body.GetBytes().Length));
        }

        [Test]
        public void Should_report_utf8_bytes_for_a_string_body()
        {
            // Same statement, same family. This one was not named in the review but carried the same
            // character count, and it is the body a hand written payload goes through.
            var body = new StringMessageBody(NonAscii);

            Assert.Multiple(() =>
            {
                Assert.That(body.Length, Is.EqualTo(Encoding.UTF8.GetByteCount(NonAscii)));
                Assert.That(body.Length, Is.EqualTo(body.GetBytes().Length));
                Assert.That(body.Length, Is.Not.EqualTo(NonAscii.Length));
            });
        }

        static SystemTextJsonObjectMessageBody ObjectBody()
        {
            return new SystemTextJsonObjectMessageBody(new { Note = NonAscii }, SystemTextJsonMessageSerializer.Options);
        }
    }


    /// <summary>
    /// The transport default writes compact JSON. Indenting is presentation, and this is broker wire
    /// data that no one reads on its way past.
    /// </summary>
    [TestFixture]
    public class Writing_the_transport_json
    {
        [Test]
        public void Should_not_indent_the_default_options()
        {
            Assert.That(SystemTextJsonMessageSerializer.Options.WriteIndented, Is.False);
        }

        [Test]
        public void Should_write_fewer_bytes_than_the_indented_form()
        {
            var message = new Sample { Id = 27, Customer = "Frank", Note = "a longer value so nesting shows" };

            var compact = JsonSerializer.Serialize(message, SystemTextJsonMessageSerializer.Options);
            var indented = JsonSerializer.Serialize(message,
                new JsonSerializerOptions(SystemTextJsonMessageSerializer.Options) { WriteIndented = true });

            Assert.That(Encoding.UTF8.GetByteCount(compact), Is.LessThan(Encoding.UTF8.GetByteCount(indented)));
        }

        [Test]
        public void Should_carry_the_same_values_as_the_indented_form()
        {
            // Compactness may not cost meaning: both forms have to read back to the same message.
            var message = new Sample { Id = 27, Customer = "Frank", Note = "a longer value so nesting shows" };

            var compact = JsonSerializer.Deserialize<Sample>(
                JsonSerializer.Serialize(message, SystemTextJsonMessageSerializer.Options),
                SystemTextJsonMessageSerializer.Options);

            Assert.Multiple(() =>
            {
                Assert.That(compact!.Id, Is.EqualTo(message.Id));
                Assert.That(compact.Customer, Is.EqualTo(message.Customer));
                Assert.That(compact.Note, Is.EqualTo(message.Note));
            });
        }


        public class Sample
        {
            public int Id { get; set; }
            public string? Customer { get; set; }
            public string? Note { get; set; }
        }
    }
}
