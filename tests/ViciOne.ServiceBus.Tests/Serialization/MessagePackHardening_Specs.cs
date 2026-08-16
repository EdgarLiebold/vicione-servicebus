namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System.Collections.Generic;
    using MessagePack;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Serialization;


    /// <summary>
    /// Every payload from a broker crosses a trust boundary, even over authenticated TLS: a credential
    /// or an authorised node can be compromised.
    /// <para>
    /// The option set is asserted directly rather than through behaviour on purpose. UntrustedData and
    /// the default both bound the object graph depth; what differs is hash collision resistance for
    /// dictionary keys, and a test that tried to demonstrate that would be a timing experiment rather
    /// than an assertion. Asserting the mode is the only form that turns red when it is dropped.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Hardening_the_message_pack_reader
    {
        [Test]
        public void Should_read_untrusted_data_with_the_hardened_security_mode()
        {
            Assert.That(InternalMessagePackResolver.Options.Security, Is.EqualTo(MessagePackSecurity.UntrustedData));
        }

        [Test]
        public void Should_resist_hash_collisions_when_reading_dictionary_keys()
        {
            // The named property behind the mode. A string keyed dictionary is exactly what a hostile
            // payload uses to force collisions, and the overlay path reads one.
            Assert.That(InternalMessagePackResolver.Options.Security.HashCollisionResistant, Is.True);
        }

        [Test]
        public void Should_read_a_string_keyed_dictionary_under_that_mode()
        {
            // The shape the overlay path reads back. It has to stay readable under the hardened mode,
            // so the hardening cannot be dropped later with "it broke the overlay" as the reason.
            var body = MessagePackSerializer.Serialize(
                new Dictionary<string, object> { ["id"] = 27, ["customer"] = "Frank" },
                InternalMessagePackResolver.Options);

            var round = MessagePackSerializer.Deserialize<Dictionary<string, object>>(
                body, InternalMessagePackResolver.Options);

            Assert.Multiple(() =>
            {
                Assert.That(round["id"], Is.EqualTo(27));
                Assert.That(round["customer"], Is.EqualTo("Frank"));
            });
        }
    }
}
