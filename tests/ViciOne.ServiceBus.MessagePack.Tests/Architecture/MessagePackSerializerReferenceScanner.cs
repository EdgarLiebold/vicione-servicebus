using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace ViciOne.ServiceBus.MessagePack.Tests.Architecture;

internal static class MessagePackSerializerReferenceScanner
{
    internal const string OwnerType = "ViciOne.ServiceBus.MessagePack.Serialization.MessagePackSerializationRuntime";
    internal const string SerializerType = "MessagePack.MessagePackSerializer";

    private static readonly Dictionary<ushort, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(instruction => unchecked((ushort)instruction.Value));

    internal static IReadOnlyList<string> FindReferences(Stream assembly, bool excludeOwner = true)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        using var reader = new PEReader(assembly, PEStreamOptions.LeaveOpen);
        var metadata = reader.GetMetadataReader();
        var offenders = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var handle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(handle);
            if (method.RelativeVirtualAddress == 0)
            {
                continue;
            }

            string declaringType = TypeName(metadata, method.GetDeclaringType());
            if (excludeOwner &&
                (declaringType == OwnerType ||
                 declaringType.StartsWith($"{OwnerType}+", StringComparison.Ordinal)))
            {
                continue;
            }

            string methodName = $"{declaringType}.{metadata.GetString(method.Name)}";
            foreach (int token in ReadMetadataTokens(reader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()))
            {
                if (ReferencedTypeName(metadata, token) == SerializerType)
                {
                    offenders.Add(methodName);
                }
            }
        }

        return [.. offenders];
    }

    private static IEnumerable<int> ReadMetadataTokens(byte[]? il)
    {
        if (il is null)
        {
            yield break;
        }

        var offset = 0;
        while (offset < il.Length)
        {
            ushort code = il[offset++];
            if (code == 0xFE)
            {
                if (offset >= il.Length)
                {
                    throw new BadImageFormatException("The IL stream ends after an opcode prefix.");
                }

                code = (ushort)(0xFE00 | il[offset++]);
            }

            if (!OpCodesByValue.TryGetValue(code, out var instruction))
            {
                throw new BadImageFormatException($"Unknown IL opcode 0x{code:X} at offset {offset - 1}.");
            }

            int operandSize = GetOperandSize(instruction.OperandType, il, offset);
            if (offset + operandSize > il.Length)
            {
                throw new BadImageFormatException($"Truncated IL operand at offset {offset}.");
            }

            if (instruction.OperandType is OperandType.InlineMethod or OperandType.InlineField or
                OperandType.InlineTok or OperandType.InlineType)
            {
                yield return BitConverter.ToInt32(il, offset);
            }

            offset += operandSize;
        }
    }

    private static int GetOperandSize(OperandType operandType, byte[] il, int offset) =>
        operandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch when offset + 4 <= il.Length =>
                checked(4 + (4 * BitConverter.ToInt32(il, offset))),
            OperandType.InlineSwitch => throw new BadImageFormatException("Truncated switch operand."),
            _ => 4,
        };

    private static string? ReferencedTypeName(MetadataReader metadata, int token)
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
        {
            return null;
        }

        return handle.Kind switch
        {
            HandleKind.MethodSpecification when IsInRange(metadata, handle, TableIndex.MethodSpec) =>
                ReferencedTypeName(
                    metadata,
                    MetadataTokens.GetToken(metadata.GetMethodSpecification((MethodSpecificationHandle)handle).Method)),
            HandleKind.MemberReference when IsInRange(metadata, handle, TableIndex.MemberRef) =>
                ParentTypeName(metadata, metadata.GetMemberReference((MemberReferenceHandle)handle).Parent),
            HandleKind.TypeReference or HandleKind.TypeDefinition => ParentTypeName(metadata, handle),
            _ => null,
        };
    }

    private static string? ParentTypeName(MetadataReader metadata, EntityHandle handle) =>
        handle.Kind switch
        {
            HandleKind.TypeReference when IsInRange(metadata, handle, TableIndex.TypeRef) =>
                Qualify(
                    metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)handle).Namespace),
                    metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)handle).Name)),
            HandleKind.TypeDefinition when IsInRange(metadata, handle, TableIndex.TypeDef) =>
                TypeName(metadata, (TypeDefinitionHandle)handle),
            _ => null,
        };

    private static string TypeName(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var definition = metadata.GetTypeDefinition(handle);
        string name = metadata.GetString(definition.Name);
        var declaring = definition.GetDeclaringType();

        return declaring.IsNil
            ? Qualify(metadata.GetString(definition.Namespace), name)
            : $"{TypeName(metadata, declaring)}+{name}";
    }

    private static string Qualify(string containingNamespace, string name) =>
        string.IsNullOrEmpty(containingNamespace) ? name : $"{containingNamespace}.{name}";

    private static bool IsInRange(MetadataReader metadata, EntityHandle handle, TableIndex table)
    {
        int row = MetadataTokens.GetRowNumber(handle);
        return row >= 1 && row <= metadata.GetTableRowCount(table);
    }
}
