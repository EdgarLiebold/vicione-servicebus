namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Context;
    using ViciOne.ServiceBus.Serialization;


    /// <summary>
    /// One invariant, stated once and applied to every message body **these assemblies declare**:
    /// <c>Length</c> is the length of <c>GetBytes()</c>, whichever accessor ran first.
    /// <para>
    /// The scope is deliberately named. This project references the abstractions, the core library and
    /// the MessagePack module, so those are the implementations it can hold to the statement; the
    /// transport bodies are covered in their own test projects, and the census case below fails if this
    /// project ever declares one that no case constructs. My earlier version counted the rows of this
    /// source instead, which are value variants rather than types, and reported thirteen types when it
    /// covered eleven.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_length_a_message_body_reports
    {
        // Four characters, seven UTF-8 bytes: one ASCII, one two byte, one three byte, one ASCII.
        const string NonAscii = "aäあb";

        // "YWJjZA==" is eight characters of Base64 carrying four bytes.
        const string Base64OfFourBytes = "YWJjZA==";

        /// <summary>
        /// The subject census: one entry per constructed body, each naming the type it stands for, so
        /// the coverage case below and the parameterized cases cannot drift apart.
        /// </summary>
        static IEnumerable<(Type Type, string Name, Func<MessageBody> Create)> BodyCases()
        {
            // One context per case, not one per body. A fresh MessageSendContext stamps its own
            // MessageId and SentTime, and the envelope carries both, so two bodies built from two
            // contexts are two different bodies whose lengths have no reason to agree. Comparing them
            // made the cross-order case fail at random; a probe that had nothing to do with the
            // envelope turned it red and that is how it surfaced.
            var context = SendContext();

            yield return (typeof(EmptyMessageBody), "empty", () => EmptyMessageBody.Instance);
            yield return (typeof(BytesMessageBody), "bytes", () => new BytesMessageBody(Encoding.UTF8.GetBytes(NonAscii)));
            yield return (typeof(BytesMessageBody), "bytes null", () => new BytesMessageBody(null));
            yield return (typeof(ArrayMessageBody), "array segment", () => new ArrayMessageBody(new ArraySegment<byte>(Encoding.UTF8.GetBytes(NonAscii))));
            yield return (typeof(MemoryMessageBody), "memory", () => new MemoryMessageBody(Encoding.UTF8.GetBytes(NonAscii)));
            yield return (typeof(StringMessageBody), "string non ascii", () => new StringMessageBody(NonAscii));
            yield return (typeof(StringMessageBody), "string whitespace", () => new StringMessageBody(" \t"));
            yield return (typeof(StringMessageBody), "string empty", () => new StringMessageBody(string.Empty));
            yield return (typeof(Base64MessageBody), "base64", () => new Base64MessageBody(Base64OfFourBytes));
            yield return (typeof(SystemTextJsonObjectMessageBody), "json object", () => new SystemTextJsonObjectMessageBody(new Note(NonAscii), SystemTextJsonMessageSerializer.Options));
            yield return (typeof(SystemTextJsonMessageBody<>), "json envelope", () => new SystemTextJsonMessageBody<Note>(context, SystemTextJsonMessageSerializer.Options));
            yield return (typeof(SystemTextJsonRawMessageBody<>), "json raw", () => new SystemTextJsonRawMessageBody<Note>(context, SystemTextJsonMessageSerializer.Options));
            yield return (typeof(MessagePackMessageBody<>), "message pack", () => new MessagePackMessageBody<Note>(context));
        }

        static IEnumerable<TestCaseData> Bodies()
        {
            foreach ((_, var name, var create) in BodyCases())
                yield return new TestCaseData(create).SetName($"{{m}}({name})");
        }

        [Test]
        public void Should_construct_every_message_body_type_these_assemblies_declare()
        {
            // The completeness statement, made against the compiler rather than against a comment. A
            // body type added to any of these three assemblies without a case here turns this red.
            // NotSupportedMessageBody is deliberately outside the invariant and has its own case, so it
            // is named here as a disposition rather than left to look like an oversight.
            Assembly[] assemblies =
            {
                typeof(MessageBody).Assembly,
                typeof(SystemTextJsonObjectMessageBody).Assembly,
                typeof(MessagePackMessageBody<>).Assembly
            };

            Type[] declared = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && typeof(MessageBody).IsAssignableFrom(type))
                .Select(Definition)
                .Distinct()
                .OrderBy(type => type.Name)
                .ToArray();

            Type[] constructed = BodyCases()
                .Select(entry => Definition(entry.Type))
                .Append(typeof(NotSupportedMessageBody))
                .Distinct()
                .ToArray();

            Assert.That(declared.Except(constructed).Select(type => type.Name), Is.Empty,
                "every message body type these assemblies declare needs a case that constructs it");
        }

        static Type Definition(Type type)
        {
            return type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type;
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
            // Writing back through the stream a caller is handed may not change what everybody else
            // reads. One of them let it. This is that one route, not a claim that the body is immutable:
            // several of these still hand a caller's own array straight back from GetBytes.
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
