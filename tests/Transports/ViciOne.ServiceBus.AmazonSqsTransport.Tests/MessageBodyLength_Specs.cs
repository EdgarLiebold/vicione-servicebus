namespace ViciOne.ServiceBus.AmazonSqsTransport.Tests
{
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Amazon.SQS.Model;
    using NUnit.Framework;


    /// <summary>
    /// The only message body in this repository that inherits its answer rather than giving one. It is
    /// covered here twice: once by running the invariant against it, and once by naming the reason the
    /// base answer is still its answer.
    /// <para>
    /// "Covered because its base type is covered" is an argument, not a proof: an override added later
    /// takes this type out of the statement while every existing test stays green. The disposition case
    /// below is what turns red in that moment.
    /// </para>
    /// <para>
    /// Needs no queue and no credentials: the body is built from a plain <see cref="Message" />.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_length_an_sqs_body_reports
    {
        // Four characters, seven UTF-8 bytes.
        const string NonAscii = "aäあb";

        [Test]
        public void Should_match_the_bytes_when_the_length_is_asked_first()
        {
            var body = new SqsMessageBody(new Message { Body = NonAscii });

            var length = body.Length;

            Assert.That(length, Is.EqualTo(body.GetBytes().LongLength));
        }

        [Test]
        public void Should_match_the_bytes_after_the_string_was_read_first()
        {
            var body = new SqsMessageBody(new Message { Body = NonAscii });

            body.GetString();

            Assert.That(body.Length, Is.EqualTo(body.GetBytes().LongLength));
        }

        [Test]
        public void Should_count_bytes_and_not_characters()
        {
            var body = new SqsMessageBody(new Message { Body = NonAscii });

            Assert.Multiple(() =>
            {
                Assert.That(body.Length, Is.EqualTo(Encoding.UTF8.GetByteCount(NonAscii)));
                Assert.That(body.Length, Is.Not.EqualTo(NonAscii.Length));
            });
        }

        [Test]
        public void Should_transmit_a_body_of_whitespace()
        {
            // The counterexample that started this, reaching the derived type through its base.
            var body = new SqsMessageBody(new Message { Body = " \t" });

            Assert.Multiple(() =>
            {
                Assert.That(body.GetBytes().Length, Is.EqualTo(2));
                Assert.That(body.Length, Is.EqualTo(2));
            });
        }

        [Test]
        public void Should_not_let_a_caller_write_through_the_stream_it_hands_out()
        {
            var body = new SqsMessageBody(new Message { Body = NonAscii });

            using Stream stream = body.GetStream();

            Assert.That(stream.CanWrite, Is.False);
        }

        [Test]
        public void Should_inherit_every_member_of_the_invariant_from_its_base()
        {
            // The disposition, asserted against the compiler rather than argued in a comment: this type
            // declares none of the four members, so the base implementation is what answers for it and
            // the base fixture is what covers it. An override introduced later turns this red and asks
            // for a fixture of its own.
            string[] declared =
            {
                nameof(MessageBody.Length), nameof(MessageBody.GetBytes),
                nameof(MessageBody.GetString), nameof(MessageBody.GetStream)
            };

            const BindingFlags OwnMembersOnly =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            Assert.Multiple(() =>
            {
                foreach (var member in declared)
                {
                    Assert.That(typeof(SqsMessageBody).GetMember(member, OwnMembersOnly), Is.Empty,
                        $"SqsMessageBody now answers {member} itself and needs its own invariant fixture");
                }

                Assert.That(typeof(SqsMessageBody).BaseType, Is.EqualTo(typeof(StringMessageBody)));
            });
        }
    }
}
