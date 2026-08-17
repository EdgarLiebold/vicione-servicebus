namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;
    using System.Reflection.PortableExecutable;
    using MessagePack;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Serialization;


    /// <summary>
    /// Every payload from a broker crosses a trust boundary, even over authenticated TLS: a credential
    /// or an authorised node can be compromised.
    /// <para>
    /// The option set is asserted directly rather than through behaviour on purpose. UntrustedData and
    /// the default both bound the object graph depth at 500; what differs is hash collision resistance
    /// for dictionary keys, and a test that tried to demonstrate that would be a timing experiment
    /// rather than an assertion. Asserting the mode is the only form that turns red when it is dropped.
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
            var body = InternalMessagePackResolver.Serialize(
                new Dictionary<string, object> { ["id"] = 27, ["customer"] = "Frank" });

            var round = InternalMessagePackResolver.Deserialize<Dictionary<string, object>>(body);

            Assert.Multiple(() =>
            {
                Assert.That(round["id"], Is.EqualTo(27));
                Assert.That(round["customer"], Is.EqualTo("Frank"));
            });
        }
    }


    /// <summary>
    /// Asserting the option set proves the set is hardened. It does not prove that the reader on the
    /// wire uses it, and that is the gap the review found: dropping the options argument from the one
    /// call in <c>MessagePackMessageBodySerializer.OverrideMessage</c> left every hardening test green,
    /// because the payload deserialized identically under the global default set. The difference
    /// between the two sets is hash collision resistance, so no behavioural assertion can separate them
    /// without engineering a collision.
    /// <para>
    /// So the boundary is asserted instead of the behaviour, and asserted where it cannot be argued
    /// with: in the compiled assembly. Exactly one type may name <see cref="MessagePackSerializer" />,
    /// and it is the type that owns the option set. Any other call site is a bypass by construction,
    /// whether it passed the wrong options or none at all.
    /// </para>
    /// <para>
    /// The scan walks call instructions and can only ever report too much, never too little: every real
    /// call sits at some offset and is examined, while a byte inside an operand that happens to look
    /// like a call almost never resolves to a member reference, and would show up immediately as a red
    /// test on green code rather than as a silent pass.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Owning_the_message_pack_option_set
    {
        const string Owner = "InternalMessagePackResolver";
        const string Serializer = "MessagePackSerializer";

        [Test]
        public void Should_name_the_serializer_only_in_the_type_that_owns_the_options()
        {
            var callers = SerializerCallersIn(typeof(InternalMessagePackResolver).Assembly.Location);

            Assert.That(callers, Is.Empty,
                $"only {Owner} may call {Serializer}; every other call site can be handed the wrong option set");
        }

        [Test]
        public void Should_find_the_owner_itself_when_it_is_not_excluded()
        {
            // The scan has to be able to see a call at all, otherwise the assertion above is green for
            // the wrong reason. The owner is the one type that legitimately makes the calls, so it is
            // also the control: without the exclusion it must be reported.
            var callers = SerializerCallersIn(typeof(InternalMessagePackResolver).Assembly.Location, excludeOwner: false);

            Assert.That(callers, Has.Some.StartsWith(Owner + "."));
        }

        static List<string> SerializerCallersIn(string assemblyPath, bool excludeOwner = true)
        {
            var callers = new List<string>();

            using var stream = File.OpenRead(assemblyPath);
            using var reader = new PEReader(stream);
            var metadata = reader.GetMetadataReader();

            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                if (method.RelativeVirtualAddress == 0)
                    continue;

                var declaringType = metadata.GetTypeDefinition(method.GetDeclaringType());
                var typeName = metadata.GetString(declaringType.Name);
                if (excludeOwner && typeName == Owner)
                    continue;

                var il = reader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
                if (il == null)
                    continue;

                for (var i = 0; i + 4 < il.Length; i++)
                {
                    const byte Call = 0x28;
                    const byte CallVirt = 0x6F;

                    if (il[i] != Call && il[i] != CallVirt)
                        continue;

                    if (CalledTypeName(metadata, BitConverter.ToInt32(il, i + 1)) != Serializer)
                        continue;

                    var caller = $"{typeName}.{metadata.GetString(method.Name)}";
                    if (!callers.Contains(caller))
                        callers.Add(caller);
                }
            }

            return callers;
        }

        static string CalledTypeName(MetadataReader metadata, int token)
        {
            EntityHandle handle;
            try
            {
                handle = MetadataTokens.EntityHandle(token);
            }
            catch (ArgumentException)
            {
                return null;
            }

            if (handle.IsNil)
                return null;

            switch (handle.Kind)
            {
                case HandleKind.MethodSpecification:
                    if (!InRange(metadata, handle, TableIndex.MethodSpec))
                        return null;

                    var specification = metadata.GetMethodSpecification((MethodSpecificationHandle)handle);
                    return CalledTypeName(metadata, MetadataTokens.GetToken(specification.Method));

                case HandleKind.MemberReference:
                    if (!InRange(metadata, handle, TableIndex.MemberRef))
                        return null;

                    var member = metadata.GetMemberReference((MemberReferenceHandle)handle);
                    if (member.Parent.Kind != HandleKind.TypeReference)
                        return null;

                    if (!InRange(metadata, member.Parent, TableIndex.TypeRef))
                        return null;

                    return metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)member.Parent).Name);

                default:
                    return null;
            }
        }

        static bool InRange(MetadataReader metadata, EntityHandle handle, TableIndex table)
        {
            var row = MetadataTokens.GetRowNumber(handle);

            return row >= 1 && row <= metadata.GetTableRowCount(table);
        }
    }
}
