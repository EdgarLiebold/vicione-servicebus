namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using MessagePack;
    using Metadata;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Serialization;


    /// <summary>
    /// Cloning a MessagePack envelope must not re-encode a payload that is already MessagePack.
    /// <para>
    /// The copy constructor set the native flag unconditionally and serialized the payload again. When
    /// the source was itself a MessagePack envelope — off the wire, or produced by an overlay — those
    /// bytes were wrapped in a second encoding, and the receiver found a byte array where the message
    /// belonged. Delayed redelivery and scheduling both clone an envelope, so a redelivered message was
    /// never consumed. Redelivery_Specs holds the end to end statement; these cases hold the act itself.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Cloning_a_message_pack_envelope
    {
        [Test]
        public void Should_keep_the_payload_bytes_of_a_native_envelope()
        {
            var native = new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }));
            var payload = native.Message;

            var clone = new MessagePackEnvelope(native);

            Assert.Multiple(() =>
            {
                Assert.That(clone.IsMessageNativeMessagePackSerialized, Is.True);
                Assert.That(clone.Message, Is.EqualTo(payload),
                    "A payload that is already MessagePack must be carried over, not encoded a second time");
            });
        }

        [Test]
        public void Should_not_share_the_payload_array_with_the_source()
        {
            // Byte identity is the contract, not reference identity. Message is public and settable and
            // holds a mutable array, so asserting that the clone is the very same array would have made
            // an aliasing defect part of the specification instead of catching it.
            var native = new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }));

            var clone = new MessagePackEnvelope(native);
            var carried = (byte[])((byte[])clone.Message).Clone();

            ((byte[])native.Message)[0] ^= 0xFF;

            Assert.Multiple(() =>
            {
                Assert.That(clone.Message, Is.Not.SameAs(native.Message), "the two envelopes share one array");
                Assert.That(clone.Message, Is.EqualTo(carried), "writing into the source changed what the clone sends");
                Assert.That(MessagePackSerializer.Deserialize<Order>((byte[])clone.Message, ContractlessOptions).Customer,
                    Is.EqualTo("Frank"), "and the clone no longer reads back");
            });
        }

        [Test]
        public void Should_stay_readable_after_being_cloned_twice()
        {
            // Each clone that re-encodes adds a layer, so reading the payload back is what tells the two
            // implementations apart rather than any byte count.
            var order = new Order { Id = 27, Customer = "Frank" };
            var clone = new MessagePackEnvelope(new MessagePackEnvelope(new MessagePackEnvelope(Foreign(order))));

            var round = MessagePackSerializer.Deserialize<Order>((byte[])clone.Message, ContractlessOptions);

            Assert.Multiple(() =>
            {
                Assert.That(round.Id, Is.EqualTo(27));
                Assert.That(round.Customer, Is.EqualTo("Frank"));
            });
        }

        [Test]
        public void Should_keep_the_overlay_form_of_a_non_native_envelope()
        {
            // The overlay path stores a serialized dictionary and clears the flag. A clone that forced
            // the flag back to true would claim a native payload for a dictionary body.
            var native = new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }));
            native.IsMessageNativeMessagePackSerialized = false;
            native.Message = MessagePackSerializer.Serialize(
                new Dictionary<string, object> { ["id"] = 27 }, ContractlessOptions);
            var payload = native.Message;

            var clone = new MessagePackEnvelope(native);

            Assert.Multiple(() =>
            {
                Assert.That(clone.IsMessageNativeMessagePackSerialized, Is.False);
                Assert.That(clone.Message, Is.EqualTo(payload));
            });
        }

        [Test]
        public void Should_still_encode_a_foreign_envelope()
        {
            // The conversion of an envelope that is not MessagePack is unchanged; only the clone of one
            // that already is was wrong.
            var clone = new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }));

            Assert.Multiple(() =>
            {
                Assert.That(clone.IsMessageNativeMessagePackSerialized, Is.True);
                Assert.That(clone.Message, Is.InstanceOf<byte[]>());
            });
        }

        [Test]
        public void Should_carry_the_metadata_of_the_source()
        {
            var source = new MessagePackEnvelope(Foreign(new Order { Id = 27, Customer = "Frank" }));
            source.MessageId = Guid.NewGuid().ToString();
            source.Headers!["Baggage"] = "kept";

            var clone = new MessagePackEnvelope(source);

            Assert.Multiple(() =>
            {
                Assert.That(clone.MessageId, Is.EqualTo(source.MessageId));
                Assert.That(clone.Headers!["Baggage"], Is.EqualTo("kept"));
                Assert.That(clone.Headers, Is.Not.SameAs(source.Headers),
                    "Headers are copied, so a later change to one envelope cannot reach the other");
            });
        }

        static MessagePackSerializerOptions ContractlessOptions =>
            MessagePackSerializerOptions.Standard
                .WithResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance)
                .WithSecurity(MessagePackSecurity.UntrustedData);

        static MessageEnvelope Foreign(object message) => new ForeignEnvelope(message);


        /// <summary>An envelope that is not a MessagePack one, so the conversion path is taken.</summary>
        class ForeignEnvelope :
            MessageEnvelope
        {
            public ForeignEnvelope(object message)
            {
                Message = message;
            }

            public string MessageId { get; } = Guid.NewGuid().ToString();
            public string RequestId => null;
            public string CorrelationId => null;
            public string ConversationId => null;
            public string InitiatorId => null;
            public string SourceAddress => null;
            public string DestinationAddress => null;
            public string ResponseAddress => null;
            public string FaultAddress => null;
            public string[] MessageType { get; } = { "urn:message:Order" };
            public object Message { get; }
            public DateTime? ExpirationTime => null;
            public DateTime? SentTime { get; } = DateTime.UtcNow;
            public Dictionary<string, object> Headers { get; } = new();
            public HostInfo Host => null;
        }


        public class Order
        {
            public int Id { get; set; }
            public string Customer { get; set; }
        }
    }
}
