namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System.Text;
    using Apache.NMS.ActiveMQ.Commands;
    using NUnit.Framework;


    /// <summary>
    /// The same invariant the core bodies are held to: the length a body reports is the length of what
    /// <c>GetBytes()</c> returns, whichever accessor ran first.
    /// <para>
    /// This body was not in the review's list. I found it by enumerating every implementation of the
    /// interface instead of only the ones named to me, and it carried the same defect in a sharper
    /// form: it reported the cached array, so the length was nothing at all until somebody had already
    /// read the body. A transport that asks for the size before reading got no answer, and the same
    /// message answered differently depending on the order of two independent calls.
    /// </para>
    /// <para>
    /// The provider's own message classes are used here, so this needs no broker and no test double.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_length_an_active_mq_body_reports
    {
        // Four characters, seven UTF-8 bytes.
        const string NonAscii = "aäあb";

        [Test]
        public void Should_answer_for_a_text_message_before_anything_was_read()
        {
            var body = new ActiveMqMessageBody(new ActiveMQTextMessage { Text = NonAscii });

            var length = body.Length;

            Assert.That(length, Is.EqualTo(body.GetBytes().LongLength));
        }

        [Test]
        public void Should_answer_for_a_text_message_after_the_bytes_were_read()
        {
            var body = new ActiveMqMessageBody(new ActiveMQTextMessage { Text = NonAscii });

            var bytes = body.GetBytes();

            Assert.That(body.Length, Is.EqualTo(bytes.LongLength));
        }

        [Test]
        public void Should_answer_for_a_bytes_message_before_anything_was_read()
        {
            // Reset is what a received message has already been through; without it the provider holds
            // the body write-only and refuses to hand it out at all.
            var message = new ActiveMQBytesMessage { Content = Encoding.UTF8.GetBytes(NonAscii) };
            message.Reset();

            var body = new ActiveMqMessageBody(message);

            var length = body.Length;

            Assert.That(length, Is.EqualTo(body.GetBytes().LongLength));
        }

        [Test]
        public void Should_count_bytes_and_not_characters()
        {
            var body = new ActiveMqMessageBody(new ActiveMQTextMessage { Text = NonAscii });

            Assert.Multiple(() =>
            {
                Assert.That(body.Length, Is.EqualTo(Encoding.UTF8.GetByteCount(NonAscii)));
                Assert.That(body.Length, Is.Not.EqualTo(NonAscii.Length));
            });
        }

        [Test]
        public void Should_report_the_same_length_in_both_access_orders()
        {
            var untouched = new ActiveMqMessageBody(new ActiveMQTextMessage { Text = NonAscii });

            var readFirst = new ActiveMqMessageBody(new ActiveMQTextMessage { Text = NonAscii });
            readFirst.GetBytes();

            Assert.That(untouched.Length, Is.EqualTo(readFirst.Length));
        }

        [Test]
        public void Should_refuse_a_message_this_transport_cannot_carry_from_both_members()
        {
            // A body that cannot be read has no length either, and it refuses both the same way. An
            // implementation that answers one of the two members and throws from the other has already
            // let them drift apart.
            var body = new ActiveMqMessageBody(new ActiveMQMapMessage());

            Assert.Multiple(() =>
            {
                Assert.That(() => body.Length, Throws.TypeOf<ActiveMqTransportException>());
                Assert.That(() => body.GetBytes(), Throws.TypeOf<ActiveMqTransportException>());
            });
        }
    }
}
