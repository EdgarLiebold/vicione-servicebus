using System.Reflection;
using System.Reflection.Emit;
using MessagePack;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Architecture;

public sealed class MessagePackSerializerBoundaryTests
{
    private const string ForeignNamespace = "ViciOne.Foreign";

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ARCHITECTURE", "single-options-owner")]
    public void ProductAssembly_NamesSerializerOnlyInsideTheOptionsOwner()
    {
        using var assembly = File.OpenRead(typeof(MessagePackSerializerFactory).Assembly.Location);

        var offenders = MessagePackSerializerReferenceScanner.FindReferences(assembly);

        Assert.Empty(offenders);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ARCHITECTURE", "scanner-positive-control")]
    public void Scanner_SeesTheRealOwnerWhenExclusionIsDisabled()
    {
        using var assembly = File.OpenRead(typeof(MessagePackSerializerFactory).Assembly.Location);

        var references = MessagePackSerializerReferenceScanner.FindReferences(assembly, excludeOwner: false);

        Assert.Contains(
            references,
            reference => reference.StartsWith(
                $"{MessagePackSerializerReferenceScanner.OwnerType}.",
                StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ReferenceShape.DirectCall, "Direct")]
    [InlineData(ReferenceShape.MethodGroup, "Deferred")]
    [InlineData(ReferenceShape.TypeToken, "Reflective")]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ARCHITECTURE", "scanner-reference-shapes")]
    public void Scanner_RejectsEveryStaticSerializerReferenceShape(
        ReferenceShape shape,
        string typeName)
    {
        using var assembly = Emit(ForeignNamespace, typeName, shape);

        var references = MessagePackSerializerReferenceScanner.FindReferences(assembly);

        Assert.Equal([$"{ForeignNamespace}.{typeName}.Run"], references);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ARCHITECTURE", "owner-qualified-by-namespace")]
    public void Scanner_DoesNotExcludeAnOwnerSimpleNameInAnotherNamespace()
    {
        using var assembly = Emit(ForeignNamespace, "MessagePackSerializationRuntime", ReferenceShape.DirectCall);

        var references = MessagePackSerializerReferenceScanner.FindReferences(assembly);

        Assert.Equal([$"{ForeignNamespace}.MessagePackSerializationRuntime.Run"], references);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ARCHITECTURE", "owner-exclusion-is-exact")]
    public void Scanner_ExcludesOnlyTheQualifiedOwner()
    {
        string owner = MessagePackSerializerReferenceScanner.OwnerType;
        int separator = owner.LastIndexOf('.');
        using var assembly = Emit(owner[..separator], owner[(separator + 1)..], ReferenceShape.DirectCall);

        Assert.Empty(MessagePackSerializerReferenceScanner.FindReferences(assembly));
        assembly.Position = 0;
        Assert.Equal(
            [$"{owner}.Run"],
            MessagePackSerializerReferenceScanner.FindReferences(assembly, excludeOwner: false));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-ARCHITECTURE", "scanner-negative-control")]
    public void Scanner_AcceptsAnAssemblyWithoutSerializerReferences()
    {
        using var assembly = Emit(ForeignNamespace, "Innocent", ReferenceShape.None);

        Assert.Empty(MessagePackSerializerReferenceScanner.FindReferences(assembly));
    }

    public enum ReferenceShape
    {
        None,
        DirectCall,
        MethodGroup,
        TypeToken,
    }

    private static MemoryStream Emit(string typeNamespace, string typeName, ReferenceShape shape)
    {
        var assembly = new PersistedAssemblyBuilder(new AssemblyName($"GateProbe-{Guid.NewGuid():N}"), typeof(object).Assembly);
        var type = assembly.DefineDynamicModule("main")
            .DefineType($"{typeNamespace}.{typeName}", TypeAttributes.Public);
        var il = type.DefineMethod(
                "Run",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(void),
                Type.EmptyTypes)
            .GetILGenerator();

        EmitBody(il, shape);
        _ = type.CreateType();

        var stream = new MemoryStream();
        assembly.Save(stream);
        stream.Position = 0;
        return stream;
    }

    private static void EmitBody(ILGenerator il, ReferenceShape shape)
    {
        MethodInfo serialize = typeof(MessagePackSerializer)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(MessagePackSerializer.Serialize) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 3 &&
                method.GetParameters()[1].ParameterType == typeof(MessagePackSerializerOptions))
            .MakeGenericMethod(typeof(object));

        switch (shape)
        {
            case ReferenceShape.None:
                break;
            case ReferenceShape.DirectCall:
                var cancellation = il.DeclareLocal(typeof(CancellationToken));
                il.Emit(OpCodes.Ldloca_S, cancellation);
                il.Emit(OpCodes.Initobj, typeof(CancellationToken));
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldloc, cancellation);
                il.Emit(OpCodes.Call, serialize);
                il.Emit(OpCodes.Pop);
                break;
            case ReferenceShape.MethodGroup:
                il.Emit(OpCodes.Ldftn, serialize);
                il.Emit(OpCodes.Pop);
                break;
            case ReferenceShape.TypeToken:
                il.Emit(OpCodes.Ldtoken, typeof(MessagePackSerializer));
                il.Emit(OpCodes.Pop);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }

        il.Emit(OpCodes.Ret);
    }
}
