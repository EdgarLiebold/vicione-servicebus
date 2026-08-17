namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Context;
    using ViciOne.ServiceBus.Serialization;


    /// <summary>
    /// One invariant, stated once and applied to every message body this repository ships:
    /// <c>Length</c> is the length of <c>GetBytes()</c>, whichever accessor ran first.
    /// <para>
    /// My earlier correction only replaced a character count with a byte count in the four bodies that
    /// had been named to me, and I tested the two that were easiest to construct. That is the wrong
    /// shape of proof: the statement is a property of the interface, so it has to be asserted against
    /// every implementation of the interface and in every accessor order. Written that way it
    /// immediately catches the two bodies the review found by hand, both of which my previous version
    /// passed over.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_length_a_message_body_reports
    {
        // Four characters, seven UTF-8 bytes: one ASCII, one two byte, one three byte, one ASCII.
        const string NonAscii = "aäあb";

        // "YWJjZA==" is eight characters of Base64 carrying four bytes.
        const string Base64OfFourBytes = "YWJjZA==";

        static IEnumerable<TestCaseData> Bodies()
        {
            // One context per case, not one per body. A fresh MessageSendContext stamps its own
            // MessageId and SentTime, and the envelope carries both, so two bodies built from two
            // contexts are two different bodies whose lengths have no reason to agree. Comparing them
            // made the cross-order case fail at random; a probe that had nothing to do with the
            // envelope turned it red and that is how it surfaced.
            var context = SendContext();

            yield return Case("empty", () => EmptyMessageBody.Instance);
            yield return Case("bytes", () => new BytesMessageBody(Encoding.UTF8.GetBytes(NonAscii)));
            yield return Case("bytes null", () => new BytesMessageBody(null));
            yield return Case("array segment", () => new ArrayMessageBody(new ArraySegment<byte>(Encoding.UTF8.GetBytes(NonAscii))));
            yield return Case("memory", () => new MemoryMessageBody(Encoding.UTF8.GetBytes(NonAscii)));
            yield return Case("string non ascii", () => new StringMessageBody(NonAscii));
            yield return Case("string whitespace", () => new StringMessageBody(" \t"));
            yield return Case("string empty", () => new StringMessageBody(string.Empty));
            yield return Case("base64", () => new Base64MessageBody(Base64OfFourBytes));
            yield return Case("json object", () => new SystemTextJsonObjectMessageBody(new Note(NonAscii), SystemTextJsonMessageSerializer.Options));
            yield return Case("json envelope", () => new SystemTextJsonMessageBody<Note>(context, SystemTextJsonMessageSerializer.Options));
            yield return Case("json raw", () => new SystemTextJsonRawMessageBody<Note>(context, SystemTextJsonMessageSerializer.Options));
            yield return Case("message pack", () => new MessagePackMessageBody<Note>(context));
        }

        [Test]
        [TestCaseSource(nameof(Bodies))]
        public void Should_match_the_bytes_when_the_length_is_asked_first(Func<MessageBody> create)
        {
            var body = create();

            var length = body.Length;

            Assert.That(length, Is.EqualTo(body.GetBytes().LongLength));
        }

        [Test]
        [TestCaseSource(nameof(Bodies))]
        public void Should_match_the_bytes_when_the_bytes_were_read_first(Func<MessageBody> create)
        {
            var body = create();

            var bytes = body.GetBytes();

            Assert.That(body.Length, Is.EqualTo(bytes.LongLength));
        }

        [Test]
        [TestCaseSource(nameof(Bodies))]
        public void Should_match_the_bytes_when_the_string_was_read_first(Func<MessageBody> create)
        {
            var body = create();

            body.GetString();

            Assert.That(body.Length, Is.EqualTo(body.GetBytes().LongLength));
        }

        [Test]
        [TestCaseSource(nameof(Bodies))]
        public void Should_match_the_bytes_when_the_stream_was_read_first(Func<MessageBody> create)
        {
            var body = create();

            using var stream = body.GetStream();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            Assert.Multiple(() =>
            {
                Assert.That(body.Length, Is.EqualTo(body.GetBytes().LongLength));
                Assert.That(buffer.Length, Is.EqualTo(body.GetBytes().LongLength), "the stream carries the same body");
            });
        }

        [Test]
        [TestCaseSource(nameof(Bodies))]
        public void Should_report_one_length_whatever_ran_first(Func<MessageBody> create)
        {
            var untouched = create();

            var readString = create();
            readString.GetString();

            var readBytes = create();
            readBytes.GetBytes();

            var readStream = create();
            readStream.GetStream().Dispose();

            Assert.Multiple(() =>
            {
                Assert.That(readString.Length, Is.EqualTo(untouched.Length));
                Assert.That(readBytes.Length, Is.EqualTo(untouched.Length));
                Assert.That(readStream.Length, Is.EqualTo(untouched.Length));
            });
        }

        [Test]
        public void Should_transmit_the_whitespace_a_string_body_was_given()
        {
            // The first counterexample from the review. GetBytes discarded a whitespace-only body while
            // GetString still returned it, so the two accessors described different bodies and the
            // length belonged to neither of them.
            var body = new StringMessageBody(" \t");

            Assert.Multiple(() =>
            {
                Assert.That(body.GetBytes().Length, Is.EqualTo(2));
                Assert.That(body.Length, Is.EqualTo(2));
                Assert.That(body.GetString(), Is.EqualTo(" \t"));
            });
        }

        [Test]
        public void Should_report_the_decoded_length_of_a_base64_body()
        {
            // The second counterexample. The Base64 text is about a third longer than the body it
            // carries, and the body is what goes on the wire.
            var body = new Base64MessageBody(Base64OfFourBytes);

            Assert.Multiple(() =>
            {
                Assert.That(body.Length, Is.EqualTo(4));
                Assert.That(body.Length, Is.Not.EqualTo(Base64OfFourBytes.Length));
                Assert.That(body.GetString(), Is.EqualTo(Base64OfFourBytes), "the text is still what a text transport carries");
            });
        }

        [Test]
        [TestCaseSource(nameof(Bodies))]
        public void Should_hand_out_a_read_only_stream(Func<MessageBody> create)
        {
            // A body is immutable, so nobody may write back through the stream it hands out. One of them
            // still did.
            var body = create();

            using var stream = body.GetStream();

            Assert.That(stream.CanWrite, Is.False);
        }

        [Test]
        public void Should_count_bytes_and_not_characters_for_a_non_ascii_string_body()
        {
            var body = new StringMessageBody(NonAscii);

            Assert.Multiple(() =>
            {
                Assert.That(body.Length, Is.EqualTo(Encoding.UTF8.GetByteCount(NonAscii)));
                Assert.That(body.Length, Is.Not.EqualTo(NonAscii.Length), "this body is the reason the two differ");
            });
        }

        [Test]
        public void Should_count_bytes_and_not_characters_for_a_non_ascii_json_body()
        {
            var body = new SystemTextJsonObjectMessageBody(new Note(NonAscii), SystemTextJsonMessageSerializer.Options);

            var text = body.GetString();

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain(NonAscii));
                Assert.That(body.Length, Is.EqualTo(Encoding.UTF8.GetByteCount(text)));
                Assert.That(body.Length, Is.Not.EqualTo(text.Length));
            });
        }

        [Test]
        public void Should_refuse_every_accessor_of_the_unsupported_body()
        {
            // The one implementation deliberately outside the invariant: it has no body at all, and it
            // says so from every member rather than answering one of them.
            var body = new NotSupportedMessageBody();

            Assert.Multiple(() =>
            {
                Assert.That(() => body.Length, Throws.TypeOf<NotSupportedException>());
                Assert.That(() => body.GetBytes(), Throws.TypeOf<NotSupportedException>());
                Assert.That(() => body.GetString(), Throws.TypeOf<NotSupportedException>());
                Assert.That(() => body.GetStream(), Throws.TypeOf<NotSupportedException>());
            });
        }

        static TestCaseData Case(string name, Func<MessageBody> create)
        {
            return new TestCaseData(create).SetName($"{{m}}({name})");
        }

        static MessageSendContext<Note> SendContext()
        {
            return new MessageSendContext<Note>(new Note(NonAscii));
        }


        public class Note
        {
            public Note()
            {
            }

            public Note(string text)
            {
                Text = text;
            }

            public string Text { get; set; }
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
                Assert.That(compact.Id, Is.EqualTo(message.Id));
                Assert.That(compact.Customer, Is.EqualTo(message.Customer));
                Assert.That(compact.Note, Is.EqualTo(message.Note));
            });
        }


        public class Sample
        {
            public int Id { get; set; }
            public string Customer { get; set; }
            public string Note { get; set; }
        }
    }
}
