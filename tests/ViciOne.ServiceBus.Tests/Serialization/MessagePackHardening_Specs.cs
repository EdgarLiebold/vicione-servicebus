namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;
    using System.Reflection.PortableExecutable;
    using System.Threading;
    using MessagePack;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Serialization;


    /// <summary>
    /// Every payload from a broker crosses a trust boundary, even over authenticated TLS: a credential
    /// or an authorised node can be compromised.
    /// <para>
    /// What this mode buys is defense in depth against selected attacks, chiefly forced hash collisions
    /// in keyed collections. It is not authentication and it is not a general bound on what a hostile
    /// payload can allocate or construct: UntrustedData and the default both stop at the same object
    /// graph depth of 500.
    /// </para>
    /// <para>
    /// The option set is therefore asserted directly rather than through behaviour. The one property
    /// that differs cannot be demonstrated without engineering a collision, so asserting the mode is the
    /// only form that turns red when it is dropped.
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
    /// because the payload deserialized identically under the global default set. The one property that
    /// differs is hash collision resistance, so no behavioural assertion can separate the two sets
    /// without engineering a collision.
    /// <para>
    /// So the boundary is asserted instead of the behaviour, in the compiled assembly. The guarantee
    /// enforced here, stated exactly: within
    /// <c>ViciOne.ServiceBus.MessagePack</c>, no method outside the type
    /// <c>ViciOne.ServiceBus.Serialization.InternalMessagePackResolver</c> and its nested types carries
    /// a metadata token that names <c>MessagePack.MessagePackSerializer</c> — as a call, as a method
    /// group or function pointer, as a field access, or as a type or member token handed to reflection.
    /// </para>
    /// <para>
    /// The owner is bound by namespace and name together: excluded by simple name, a second type of that
    /// name in another namespace walks straight past the rule. The instruction stream is decoded with
    /// the runtime's own opcode table rather than scanned byte by byte, so an operand that happens to
    /// look like a call is never read as one and an instruction is never missed because its operand is
    /// longer than the scan assumed. A byte scan does not only over-report; it under-reports too, and
    /// the method group probe below is exactly that case.
    /// </para>
    /// <para>
    /// What it does not cover: a call reached through a name computed at run time, or through a type
    /// this rule cannot see in this assembly. The rule catches an ordinary accidental bypass, not a
    /// determined one.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Owning_the_message_pack_option_set
    {
        internal const string Owner = "ViciOne.ServiceBus.Serialization.InternalMessagePackResolver";
        internal const string Serializer = "MessagePack.MessagePackSerializer";

        [Test]
        public void Should_name_the_serializer_only_in_the_type_that_owns_the_options()
        {
            var offenders = SerializerReferencesIn(typeof(InternalMessagePackResolver).Assembly.Location);

            Assert.That(offenders, Is.Empty,
                $"only {Owner} may name {Serializer}; every other reference can reach it without the hardened option set");
        }

        [Test]
        public void Should_report_the_owner_itself_when_it_is_not_excluded()
        {
            // The control. Without it the assertion above could be green because the rule sees nothing
            // at all.
            var references = SerializerReferencesIn(typeof(InternalMessagePackResolver).Assembly.Location, excludeOwner: false);

            Assert.That(references, Has.Some.StartsWith(Owner + "."));
        }

        /// <summary>
        /// Every method in the assembly whose body carries a token naming the serializer, reported as
        /// "declaring type.method".
        /// </summary>
        static List<string> SerializerReferencesIn(string assemblyPath, bool excludeOwner = true)
        {
            using var stream = File.OpenRead(assemblyPath);

            return SerializerReferencesIn(stream, excludeOwner);
        }

        internal static List<string> SerializerReferencesIn(Stream assembly, bool excludeOwner = true)
        {
            var offenders = new List<string>();

            using var reader = new PEReader(assembly);
            var metadata = reader.GetMetadataReader();

            foreach (var handle in metadata.MethodDefinitions)
            {
                var method = metadata.GetMethodDefinition(handle);
                if (method.RelativeVirtualAddress == 0)
                    continue;

                var declaringType = TypeName(metadata, method.GetDeclaringType());
                if (excludeOwner && (declaringType == Owner || declaringType.StartsWith(Owner + "+", StringComparison.Ordinal)))
                    continue;

                var body = reader.GetMethodBody(method.RelativeVirtualAddress);
                var name = $"{declaringType}.{metadata.GetString(method.Name)}";

                foreach (var token in TokensOf(body.GetILBytes()))
                {
                    if (ReferencedTypeName(metadata, token) != Serializer)
                        continue;

                    if (!offenders.Contains(name))
                        offenders.Add(name);
                }
            }

            return offenders;
        }

        /// <summary>
        /// Decodes the instruction stream and yields the metadata token of every instruction that
        /// carries one: a method, a field, or a type or member token. Anything that can put the
        /// serializer's name into a method body goes through one of those.
        /// </summary>
        static IEnumerable<int> TokensOf(byte[] il)
        {
            if (il == null)
                yield break;

            var offset = 0;
            while (offset < il.Length)
            {
                ushort code = il[offset++];
                if (code == 0xFE)
                {
                    if (offset >= il.Length)
                        yield break;

                    code = (ushort)(0xFE00 | il[offset++]);
                }

                if (!OpCodesByValue.TryGetValue(code, out var instruction))
                    throw new InvalidOperationException($"unknown opcode 0x{code:X} at offset {offset - 1}");

                var operandType = instruction.OperandType;
                var operandSize = OperandSize(operandType, il, offset);

                if (offset + operandSize > il.Length)
                    yield break;

                if (operandType is OperandType.InlineMethod or OperandType.InlineField
                    or OperandType.InlineTok or OperandType.InlineType)
                {
                    yield return BitConverter.ToInt32(il, offset);
                }

                offset += operandSize;
            }
        }

        static int OperandSize(OperandType operandType, byte[] il, int offset)
        {
            switch (operandType)
            {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.InlineSwitch:
                    // The only variable length operand: a count followed by that many branch targets.
                    return 4 + 4 * BitConverter.ToInt32(il, offset);
                default:
                    return 4;
            }
        }

        static readonly Dictionary<ushort, OpCode> OpCodesByValue = typeof(OpCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null))
            .ToDictionary(instruction => unchecked((ushort)instruction.Value));

        static string ReferencedTypeName(MetadataReader metadata, int token)
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

                    return ReferencedTypeName(metadata,
                        MetadataTokens.GetToken(metadata.GetMethodSpecification((MethodSpecificationHandle)handle).Method));

                case HandleKind.MemberReference:
                    if (!InRange(metadata, handle, TableIndex.MemberRef))
                        return null;

                    return ParentTypeName(metadata, metadata.GetMemberReference((MemberReferenceHandle)handle).Parent);

                case HandleKind.TypeReference:
                case HandleKind.TypeDefinition:
                    return ParentTypeName(metadata, handle);

                default:
                    return null;
            }
        }

        static string ParentTypeName(MetadataReader metadata, EntityHandle handle)
        {
            switch (handle.Kind)
            {
                case HandleKind.TypeReference when InRange(metadata, handle, TableIndex.TypeRef):
                    var reference = metadata.GetTypeReference((TypeReferenceHandle)handle);
                    return Qualify(metadata.GetString(reference.Namespace), metadata.GetString(reference.Name));

                case HandleKind.TypeDefinition when InRange(metadata, handle, TableIndex.TypeDef):
                    return TypeName(metadata, (TypeDefinitionHandle)handle);

                default:
                    return null;
            }
        }

        static string TypeName(MetadataReader metadata, TypeDefinitionHandle handle)
        {
            var definition = metadata.GetTypeDefinition(handle);
            var name = metadata.GetString(definition.Name);

            var declaring = definition.GetDeclaringType();
            if (!declaring.IsNil)
                return $"{TypeName(metadata, declaring)}+{name}";

            return Qualify(metadata.GetString(definition.Namespace), name);
        }

        static string Qualify(string containingNamespace, string name)
        {
            return string.IsNullOrEmpty(containingNamespace) ? name : $"{containingNamespace}.{name}";
        }

        static bool InRange(MetadataReader metadata, EntityHandle handle, TableIndex table)
        {
            var row = MetadataTokens.GetRowNumber(handle);

            return row >= 1 && row <= metadata.GetTableRowCount(table);
        }
    }


    /// <summary>
    /// The rule that guards the option set is itself guarded, permanently and by behaviour rather than
    /// by a statement about its own constants.
    /// <para>
    /// What matters is what the rule does when it meets each shape of bypass, so each shape is emitted
    /// into a throwaway assembly here and the rule is run against it. Asserting that the owner constant
    /// contains a dot is true of the constant and says nothing about the rule. These cases stay red if
    /// the rule is ever narrowed back to a name comparison or to a scan that only looks at call
    /// instructions.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_rule_that_guards_the_option_set
    {
        const string Elsewhere = "Some.Other.Namespace";

        [Test]
        public void Should_report_a_direct_call_from_a_foreign_type()
        {
            using var assembly = Emit(Elsewhere, "Sender", DirectCall);

            Assert.That(Owning_the_message_pack_option_set.SerializerReferencesIn(assembly),
                Has.One.EqualTo($"{Elsewhere}.Sender.Run"));
        }

        [Test]
        public void Should_report_a_type_of_the_owner_simple_name_in_another_namespace()
        {
            // A simple name in the exclusion lets this shape walk past the rule and the assembly reports
            // nothing at all, so it is emitted here rather than described.
            using var assembly = Emit(Elsewhere, "InternalMessagePackResolver", DirectCall);

            Assert.That(Owning_the_message_pack_option_set.SerializerReferencesIn(assembly),
                Has.One.EqualTo($"{Elsewhere}.InternalMessagePackResolver.Run"));
        }

        [Test]
        public void Should_report_a_method_group_that_never_calls()
        {
            // ldftn, not call. A scan that only looks for call instructions reports nothing here, which
            // is the under-reporting a byte scan is assumed not to be capable of.
            using var assembly = Emit(Elsewhere, "Deferred", MethodGroup);

            Assert.That(Owning_the_message_pack_option_set.SerializerReferencesIn(assembly),
                Has.One.EqualTo($"{Elsewhere}.Deferred.Run"));
        }

        [Test]
        public void Should_report_a_type_token_handed_to_reflection()
        {
            // ldtoken, the shape of typeof(MessagePackSerializer) on its way into a reflective call.
            using var assembly = Emit(Elsewhere, "Reflective", TypeToken);

            Assert.That(Owning_the_message_pack_option_set.SerializerReferencesIn(assembly),
                Has.One.EqualTo($"{Elsewhere}.Reflective.Run"));
        }

        [Test]
        public void Should_not_report_the_owner_itself()
        {
            var owner = Owning_the_message_pack_option_set.Owner;
            var separator = owner.LastIndexOf('.');

            using var assembly = Emit(owner[..separator], owner[(separator + 1)..], DirectCall);

            Assert.That(Owning_the_message_pack_option_set.SerializerReferencesIn(assembly), Is.Empty);
        }

        [Test]
        public void Should_report_the_owner_when_the_exclusion_is_lifted()
        {
            // The control for the case above: without it, an exclusion that swallowed everything would
            // look exactly like a clean assembly.
            var owner = Owning_the_message_pack_option_set.Owner;
            var separator = owner.LastIndexOf('.');

            using var assembly = Emit(owner[..separator], owner[(separator + 1)..], DirectCall);

            Assert.That(Owning_the_message_pack_option_set.SerializerReferencesIn(assembly, excludeOwner: false),
                Has.One.EqualTo(owner + ".Run"));
        }

        [Test]
        public void Should_report_nothing_for_an_assembly_that_never_names_the_serializer()
        {
            using var assembly = Emit(Elsewhere, "Innocent", il => il.Emit(OpCodes.Ret));

            Assert.That(Owning_the_message_pack_option_set.SerializerReferencesIn(assembly), Is.Empty);
        }

        static void DirectCall(ILGenerator il)
        {
            var cancellation = il.DeclareLocal(typeof(CancellationToken));
            il.Emit(OpCodes.Ldloca_S, cancellation);
            il.Emit(OpCodes.Initobj, typeof(CancellationToken));
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldloc, cancellation);
            il.Emit(OpCodes.Call, Serialize);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
        }

        static void MethodGroup(ILGenerator il)
        {
            il.Emit(OpCodes.Ldftn, Serialize);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
        }

        static void TypeToken(ILGenerator il)
        {
            il.Emit(OpCodes.Ldtoken, typeof(MessagePackSerializer));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
        }

        static MethodInfo Serialize => typeof(MessagePackSerializer)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(MessagePackSerializer.Serialize)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 3
                && method.GetParameters()[1].ParameterType == typeof(MessagePackSerializerOptions))
            .MakeGenericMethod(typeof(object));

        static MemoryStream Emit(string typeNamespace, string typeName, Action<ILGenerator> body)
        {
            var builder = new PersistedAssemblyBuilder(new AssemblyName("GateProbe"), typeof(object).Assembly);

            var type = builder
                .DefineDynamicModule("main")
                .DefineType($"{typeNamespace}.{typeName}", TypeAttributes.Public);

            body(type.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                Type.EmptyTypes).GetILGenerator());

            type.CreateType();

            var stream = new MemoryStream();
            builder.Save(stream);
            stream.Position = 0;

            return stream;
        }
    }
}
