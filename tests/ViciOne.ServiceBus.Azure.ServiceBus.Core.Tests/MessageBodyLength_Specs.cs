namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using AzureServiceBusTransport;
    using NUnit.Framework;


    /// <summary>
    /// The same invariant every other message body is held to: the length it reports is the length of
    /// what <c>GetBytes()</c> returns, whichever accessor ran first, and the stream it hands out cannot
    /// be written through.
    /// <para>
    /// This type was missing from my census. I had reported that every implementation was enumerated
    /// while counting the rows of one test source, and those rows are value variants rather than types.
    /// The implementation itself turned out to be correct, which is the point: a census is a statement
    /// about coverage, and it was wrong even though nothing was broken behind it.
    /// </para>
    /// <para>
    /// It needs no namespace, no connection and no emulator: the body is built from a
    /// <see cref="BinaryData" /> value.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_length_a_service_bus_body_reports
    {
        // Four characters, seven UTF-8 bytes.
        const string NonAscii = "aäあb";

        [Test]
        public void Should_match_the_bytes_when_the_length_is_asked_first()
        {
            var body = new ServiceBusMessageBody(BinaryData.FromBytes(Encoding.UTF8.GetBytes(NonAscii)));

            var length = body.Length;

            Assert.That(length, Is.EqualTo(body.GetBytes().LongLength));
        }

        [Test]
        public void Should_match_the_bytes_after_every_other_accessor_ran_first()
        {
            var payload = Encoding.UTF8.GetBytes(NonAscii);

            var readString = new ServiceBusMessageBody(BinaryData.FromBytes(payload));
            readString.GetString();

            var readBytes = new ServiceBusMessageBody(BinaryData.FromBytes(payload));
            readBytes.GetBytes();

            var readStream = new ServiceBusMessageBody(BinaryData.FromBytes(payload));
            readStream.GetStream().Dispose();

            Assert.Multiple(() =>
            {
                Assert.That(readString.Length, Is.EqualTo(readString.GetBytes().LongLength));
                Assert.That(readBytes.Length, Is.EqualTo(readBytes.GetBytes().LongLength));
                Assert.That(readStream.Length, Is.EqualTo(readStream.GetBytes().LongLength));
            });
        }

        [Test]
        public void Should_count_bytes_and_not_characters()
        {
            var body = new ServiceBusMessageBody(BinaryData.FromBytes(Encoding.UTF8.GetBytes(NonAscii)));

            Assert.Multiple(() =>
            {
                Assert.That(body.Length, Is.EqualTo(Encoding.UTF8.GetByteCount(NonAscii)));
                Assert.That(body.Length, Is.Not.EqualTo(NonAscii.Length));
            });
        }

        [Test]
        public void Should_report_nothing_for_an_empty_body()
        {
            var body = new ServiceBusMessageBody(BinaryData.FromBytes(Array.Empty<byte>()));

            Assert.That(body.Length, Is.Zero);
        }

        [Test]
        public void Should_not_let_a_caller_write_through_the_stream_it_hands_out()
        {
            // Not a claim that the body is immutable: the array handed out by GetBytes is still a
            // mutable array. What is prevented is writing back through the stream.
            var body = new ServiceBusMessageBody(BinaryData.FromBytes(Encoding.UTF8.GetBytes(NonAscii)));

            using Stream stream = body.GetStream();

            Assert.That(stream.CanWrite, Is.False);
        }

        [Test]
        public void Should_carry_the_same_bytes_through_the_stream()
        {
            var body = new ServiceBusMessageBody(BinaryData.FromBytes(Encoding.UTF8.GetBytes(NonAscii)));

            using var stream = body.GetStream();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            Assert.That(buffer.ToArray(), Is.EqualTo(body.GetBytes()));
        }
    }
}
