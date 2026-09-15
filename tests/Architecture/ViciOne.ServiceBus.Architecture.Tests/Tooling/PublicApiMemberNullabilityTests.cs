using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Build;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Tooling;

public sealed class PublicApiMemberNullabilityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-distinguishes-nullable-and-required-parameters")]
    public void ParameterNullability_DistinguishesNullableAndRequiredReferences()
    {
        Assert.Equal(typeof(RequiredReference).GetMethod("Accept")!.GetParameters()[0].ParameterType,
            typeof(NullableReference).GetMethod("Accept")!.GetParameters()[0].ParameterType);
        Assert.Equal("METHOD public System.Void Accept(System.String value)", Method(typeof(RequiredReference), "Accept"));
        Assert.Equal("METHOD public System.Void Accept(System.String value [nullability={read=Nullable;write=Nullable}])",
            Method(typeof(NullableReference), "Accept"));
        Assert.NotEqual(Method(typeof(RequiredReference), "Accept"), Method(typeof(NullableReference), "Accept"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-distinguishes-nullable-and-required-returns")]
    public void ReturnNullability_DistinguishesNullableAndRequiredReferences()
    {
        Assert.Equal("METHOD public System.String Read()", Method(typeof(RequiredReference), "Read"));
        Assert.Equal("METHOD public System.String Read() [return-nullability={read=Nullable;write=Nullable}]",
            Method(typeof(NullableReference), "Read"));
        Assert.NotEqual(Method(typeof(RequiredReference), "Read"), Method(typeof(NullableReference), "Read"));
    }

    [Theory]
    [InlineData("First", " [nullability={read=NotNull;write=NotNull;arguments=[{read=Nullable;write=Nullable},{read=NotNull;write=NotNull;arguments=[{read=NotNull;write=NotNull}]}]}]")]
    [InlineData("Second", " [nullability={read=NotNull;write=NotNull;arguments=[{read=NotNull;write=NotNull},{read=NotNull;write=NotNull;arguments=[{read=Nullable;write=Nullable}]}]}]")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-nested-generic-argument-positions")]
    public void ParameterNullability_PreservesNestedGenericArgumentPositions(string name, string expected)
    {
        ParameterInfo parameter = typeof(GenericPositions).GetMethod(name)!.GetParameters()[0];
        NullabilityInfo info = new NullabilityInfoContext().Create(parameter);
        Assert.Equal(typeof(KeyValuePair<string, List<string>>), parameter.ParameterType);
        Assert.Equal(2, info.GenericTypeArguments.Length);
        Assert.Equal(name == "First" ? NullabilityState.Nullable : NullabilityState.NotNull,
            info.GenericTypeArguments[0].ReadState);
        Assert.Equal(name == "Second" ? NullabilityState.Nullable : NullabilityState.NotNull,
            Assert.Single(info.GenericTypeArguments[1].GenericTypeArguments).ReadState);
        Assert.Equal("METHOD public System.Void " + name +
            "(System.Collections.Generic.KeyValuePair<System.String,System.Collections.Generic.List<System.String>> value" + expected + ")",
            Method(typeof(GenericPositions), name));
    }

    [Theory]
    [InlineData("NullableArray", "{read=Nullable;write=Nullable;element={read=NotNull;write=NotNull}}")]
    [InlineData("NullableElement", "{read=NotNull;write=NotNull;element={read=Nullable;write=Nullable}}")]
    [InlineData("NullableBoth", "{read=Nullable;write=Nullable;element={read=Nullable;write=Nullable}}")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-array-root-and-element-nodes")]
    public void ArrayNullability_PreservesRootAndElementNodes(string name, string expected)
    {
        ParameterInfo parameter = typeof(Arrays).GetMethod(name)!.GetParameters()[0];
        Assert.Equal(typeof(string[]), parameter.ParameterType);
        Assert.NotNull(new NullabilityInfoContext().Create(parameter).ElementType);
        Assert.Equal("METHOD public System.Void " + name + "(System.String[] value [nullability=" + expected + "])",
            Method(typeof(Arrays), name));
    }

    [Theory]
    [InlineData("Jagged", "System.String[][]", "{read=Nullable;write=Nullable;element={read=Nullable;write=Nullable;element={read=Nullable;write=Nullable}}}")]
    [InlineData("Matrix", "System.String[,]", "{read=Nullable;write=Nullable;element={read=Nullable;write=Nullable}}")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-jagged-and-multidimensional-array-nodes")]
    public void ArrayNullability_PreservesJaggedAndMultidimensionalNodes(string name, string type, string expected)
        => Assert.Equal("METHOD public System.Void " + name + "(" + type + " value [nullability=" + expected + "])",
            Method(typeof(Arrays), name));

    [Theory]
    [InlineData("AllowsNull", NullabilityState.NotNull, NullabilityState.Nullable)]
    [InlineData("MayBeNull", NullabilityState.Nullable, NullabilityState.NotNull)]
    [InlineData("Nullable", NullabilityState.Nullable, NullabilityState.Nullable)]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-distinct-property-read-and-write-promises")]
    public void PropertyNullability_PreservesDistinctReadAndWritePromises(string name, NullabilityState read, NullabilityState write)
    {
        NullabilityInfo info = new NullabilityInfoContext().Create(typeof(Properties).GetProperty(name)!);
        Assert.Equal(read, info.ReadState);
        Assert.Equal(write, info.WriteState);
        Assert.Equal("PROPERTY System.String " + name + " { public-get; public-set; } [nullability={read=" + read + ";write=" + write + "}]",
            Member(typeof(Properties), "PROPERTY ", name));
    }

    [Theory]
    [InlineData("AllowsNull", NullabilityState.NotNull, NullabilityState.Nullable)]
    [InlineData("MayBeNull", NullabilityState.Nullable, NullabilityState.NotNull)]
    [InlineData("Nullable", NullabilityState.Nullable, NullabilityState.Nullable)]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-distinct-field-read-and-write-promises")]
    public void FieldNullability_PreservesDistinctReadAndWritePromises(string name, NullabilityState read, NullabilityState write)
    {
        NullabilityInfo info = new NullabilityInfoContext().Create(typeof(Fields).GetField(name)!);
        Assert.Equal(read, info.ReadState);
        Assert.Equal(write, info.WriteState);
        Assert.Equal("FIELD public System.String " + name + " [nullability={read=" + read + ";write=" + write + "}]",
            Member(typeof(Fields), "FIELD ", name));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-indexer-value-and-index-parameter-contracts")]
    public void IndexerNullability_PreservesValueAndIndexParameterContracts()
        => Assert.Equal(
            "PROPERTY System.String Item[System.String index [nullability={read=Nullable;write=Nullable}]] { public-get; public-set; } [nullability={read=NotNull;write=Nullable}]",
            Member(typeof(Indexed), "PROPERTY ", "Item"));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-constructor-parameter-contracts")]
    public void ConstructorNullability_PreservesParameterContracts()
        => Assert.Equal(
            "CTOR public ViciOne.ServiceBus.Architecture.Tests.Tooling.PublicApiMemberNullabilityTests.Constructed(System.String value [nullability={read=Nullable;write=Nullable}])",
            Assert.Single(PublicApiBaseline.FormatMembers(typeof(Constructed)), row => row.StartsWith("CTOR ", StringComparison.Ordinal)));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-event-delegate-and-payload-contracts")]
    public void EventNullability_PreservesDelegateAndPayloadContracts()
    {
        NullabilityInfo info = new NullabilityInfoContext().Create(typeof(Events).GetEvent("Changed")!);
        Assert.Equal(NullabilityState.Nullable, info.ReadState);
        Assert.Equal(NullabilityState.Nullable, Assert.Single(info.GenericTypeArguments).ReadState);
        Assert.Equal(
            "EVENT System.Action<System.String> Changed { public-add; public-remove; } [nullability={read=Nullable;write=Nullable;arguments=[{read=Nullable;write=Nullable}]}]",
            Member(typeof(Events), "EVENT ", "Changed"));
    }

    [Theory]
    [InlineData("Reference", "ref ")]
    [InlineData("Input", "in ")]
    [InlineData("Location", "ref readonly ")]
    [InlineData("Output", "out ")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-byref-element-contracts")]
    public void ByReferenceNullability_PreservesElementContracts(string name, string modifier)
    {
        ParameterInfo parameter = typeof(References).GetMethod(name)!.GetParameters()[0];
        Assert.True(parameter.ParameterType.IsByRef);
        Assert.Equal(typeof(string), parameter.ParameterType.GetElementType());
        NullabilityInfo info = new NullabilityInfoContext().Create(parameter);
        Assert.Equal(NullabilityState.Nullable, info.ReadState);
        Assert.Equal(NullabilityState.Nullable, info.WriteState);
        string row = Method(typeof(References), name);
        Assert.StartsWith("METHOD public System.Void " + name + "(" + modifier + "System.String value", row, StringComparison.Ordinal);
        Assert.Contains(" [nullability={read=Nullable;write=Nullable}]", row, StringComparison.Ordinal);
    }

    [Theory]
    // Generic declaration constraints and reflected member-use states are separate metadata contracts.
    [InlineData("Required", false, NullabilityState.Unknown)]
    [InlineData("Nullable", true, NullabilityState.Nullable)]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-generic-parameter-use-sites")]
    public void GenericParameterNullability_PreservesUseSiteContracts(string name, bool nullable, NullabilityState state)
    {
        Type type = nullable ? typeof(NullableGenericUse) : typeof(RequiredGenericUse);
        MethodInfo method = type.GetMethod(name)!;
        NullabilityInfoContext context = new();
        Assert.Equal(state, context.Create(method.GetParameters()[0]).ReadState);
        Assert.Equal(state, context.Create(method.ReturnParameter).ReadState);
        string parameter = " [nullability={read=" + state + ";write=" + state + "}]";
        string result = " [return-nullability={read=" + state + ";write=" + state + "}]";
        Assert.Equal("METHOD public T " + name + "<T>(T value" + parameter +
            ") [generic=T{flags=ReferenceTypeConstraint;constraints=[];nullable=[1];unmanaged=false}]" + result,
            Method(type, name));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-distinguishes-oblivious-and-known-reference-contracts")]
    public void ReferenceNullability_DistinguishesObliviousAndKnownContracts()
    {
        ParameterInfo parameter = typeof(ObliviousReference).GetMethod("Accept")!.GetParameters()[0];
        Assert.Equal(NullabilityState.Unknown, new NullabilityInfoContext().Create(parameter).ReadState);
        Assert.Equal("METHOD public System.Void Accept(System.String value [nullability={read=Unknown;write=Unknown}])",
            Method(typeof(ObliviousReference), "Accept"));
        Assert.NotEqual(Method(typeof(RequiredReference), "Accept"), Method(typeof(ObliviousReference), "Accept"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-excludes-private-property-accessor-promises")]
    public void PropertyNullability_ExcludesPrivateAccessorPromises()
    {
        NullabilityInfo info = new NullabilityInfoContext().Create(typeof(PrivateSetter).GetProperty("Value")!);
        Assert.Equal(NullabilityState.NotNull, info.ReadState);
        Assert.Equal(NullabilityState.Nullable, info.WriteState);
        Assert.Equal("PROPERTY System.String Value { public-get;  }", Member(typeof(PrivateSetter), "PROPERTY ", "Value"));
    }

    [Theory]
    [InlineData(typeof(ReadOnlyNullable), "Value", "{ public-get;  } [nullability={read=Nullable;write=none}]")]
    [InlineData(typeof(WriteOnlyNullable), "Value", "{  public-set; } [nullability={read=none;write=Nullable}]")]
    [InlineData(typeof(PrivateReader), "Value", "{  public-set; }")]
    [InlineData(typeof(ReadOnlyElements), "Values", "{ public-get;  } [nullability={read=NotNull;write=none;arguments=[{read=Nullable;write=Nullable}]}]")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-only-externally-visible-property-directions")]
    public void PropertyNullability_PreservesOnlyExternallyVisibleDirections(Type type, string name, string expected)
    {
        string valueType = type == typeof(ReadOnlyElements) ? "System.Collections.Generic.List<System.String>" : "System.String";
        Assert.Equal("PROPERTY " + valueType + " " + name + " " + expected, Member(type, "PROPERTY ", name));
    }

    [Theory]
    [InlineData("AllowsNull", NullabilityState.NotNull, NullabilityState.Nullable)]
    [InlineData("DisallowsNull", NullabilityState.Nullable, NullabilityState.NotNull)]
    [InlineData("EnsuresNotNull", NullabilityState.NotNull, NullabilityState.Nullable)]
    [InlineData("MayBeNull", NullabilityState.Nullable, NullabilityState.NotNull)]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-parameter-flow-read-and-write-promises")]
    public void ParameterNullability_PreservesFlowReadAndWritePromises(string name, NullabilityState read, NullabilityState write)
    {
        NullabilityInfo info = new NullabilityInfoContext().Create(typeof(Flow).GetMethod(name)!.GetParameters()[0]);
        Assert.Equal(read, info.ReadState);
        Assert.Equal(write, info.WriteState);
        Assert.Equal("METHOD public System.Void " + name + "(System.String value [nullability={read=" + read + ";write=" + write + "}])",
            Method(typeof(Flow), name));
    }

    [Theory]
    [InlineData("NullableResult", NullabilityState.Nullable, NullabilityState.NotNull)]
    [InlineData("RequiredResult", NullabilityState.NotNull, NullabilityState.Nullable)]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-return-flow-read-and-write-promises")]
    public void ReturnNullability_PreservesFlowReadAndWritePromises(string name, NullabilityState read, NullabilityState write)
    {
        NullabilityInfo info = new NullabilityInfoContext().Create(typeof(Flow).GetMethod(name)!.ReturnParameter);
        Assert.Equal(read, info.ReadState);
        Assert.Equal(write, info.WriteState);
        Assert.Equal("METHOD public System.String " + name + "() [return-nullability={read=" + read + ";write=" + write + "}]",
            Method(typeof(Flow), name));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-preserves-nullable-value-flow-refinements")]
    public void NullableValueFlow_PreservesDirectionRefinementsWithoutInventingOrdinaryContracts()
    {
        NullabilityInfoContext context = new();
        NullabilityInfo parameter = context.Create(typeof(ValueFlow).GetMethod("WriteRestricted")!.GetParameters()[0]);
        NullabilityInfo result = context.Create(typeof(ValueFlow).GetMethod("ResultRequired")!.ReturnParameter);
        Assert.Equal(NullabilityState.Nullable, parameter.ReadState);
        Assert.Equal(NullabilityState.NotNull, parameter.WriteState);
        Assert.Equal(NullabilityState.NotNull, result.ReadState);
        Assert.Equal(NullabilityState.Nullable, result.WriteState);
        Assert.Equal("METHOD public System.Void WriteRestricted(System.Nullable<System.Guid> value [nullability={read=Nullable;write=NotNull}])",
            Method(typeof(ValueFlow), "WriteRestricted"));
        Assert.Equal("METHOD public System.Nullable<System.Guid> ResultRequired() [return-nullability={read=NotNull;write=Nullable}]",
            Method(typeof(ValueFlow), "ResultRequired"));
        Assert.Equal("METHOD public System.Nullable<System.Guid> Ordinary(System.Nullable<System.Guid> value)",
            Method(typeof(ValueFlow), "Ordinary"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-nullability-does-not-invent-reference-contracts-for-value-only-members")]
    public void ValueOnlyMembers_DoNotInventReferenceNullabilityContracts()
    {
        string[] rows = PublicApiBaseline.FormatMembers(typeof(ValueOnly)).ToArray();
        Assert.Contains("METHOD public System.Int32 Read(System.Nullable<System.Guid> value)", rows);
        Assert.DoesNotContain(rows, row => row.Contains("nullability=", StringComparison.Ordinal));
    }

    private static string Method(Type type, string name)
        => Assert.Single(PublicApiBaseline.FormatMembers(type), row => row.StartsWith("METHOD ", StringComparison.Ordinal)
            && (row.Contains(" " + name + "(", StringComparison.Ordinal) || row.Contains(" " + name + "<", StringComparison.Ordinal)));

    private static string Member(Type type, string kind, string name)
        => Assert.Single(PublicApiBaseline.FormatMembers(type), row => row.StartsWith(kind, StringComparison.Ordinal)
            && (row.Contains(" " + name + " ", StringComparison.Ordinal) || row.Contains(" " + name + "[", StringComparison.Ordinal)
                || row.EndsWith(" " + name, StringComparison.Ordinal)));

    private sealed class RequiredReference { public void Accept(string value) { } public string Read() => string.Empty; }
    private sealed class NullableReference { public void Accept(string? value) { } public string? Read() => null; }
    private sealed class GenericPositions
    {
        public void First(KeyValuePair<string?, List<string>> value) { }
        public void Second(KeyValuePair<string, List<string?>> value) { }
    }
    private sealed class Arrays
    {
        public void NullableArray(string[]? value) { }
        public void NullableElement(string?[] value) { }
        public void NullableBoth(string?[]? value) { }
        public void Jagged(string?[]?[]? value) { }
        public void Matrix(string?[,]? value) { }
    }
    private sealed class Properties
    {
        [AllowNull] public string AllowsNull { get; set; } = string.Empty;
        [MaybeNull] public string MayBeNull { get; set; } = string.Empty;
        public string? Nullable { get; set; }
    }
    private sealed class Fields
    {
        [AllowNull] public string AllowsNull = string.Empty;
        [MaybeNull] public string MayBeNull = string.Empty;
        public string? Nullable = string.Empty;
    }
    private sealed class Indexed { [AllowNull] public string this[string? index] { get => string.Empty; set { } } }
    private sealed class Constructed { public Constructed(string? value) { } }
    private sealed class Events { public event Action<string?>? Changed { add { } remove { } } }
    private sealed class References
    {
        public void Reference(ref string? value) { }
        public void Input(in string? value) { }
        public void Location(ref readonly string? value) { }
        public void Output(out string? value) => value = null;
    }
    private sealed class RequiredGenericUse
    {
        public T Required<T>(T value) where T : class => value;
    }
    private sealed class NullableGenericUse
    {
        public T? Nullable<T>(T? value) where T : class => value;
    }
    private sealed class PrivateSetter { [AllowNull] public string Value { get => string.Empty; private set { } } }
    private sealed class ReadOnlyNullable { public string? Value => null; }
    private sealed class WriteOnlyNullable { public string? Value { set { } } }
    private sealed class PrivateReader { [MaybeNull] public string Value { private get => string.Empty; set { } } }
    private sealed class ReadOnlyElements { public List<string?> Values => []; }
    private sealed class Flow
    {
        public void AllowsNull([AllowNull] string value) { }
        public void DisallowsNull([DisallowNull] string? value) { }
        public void EnsuresNotNull([NotNull] string? value) => value = string.Empty;
        public void MayBeNull([MaybeNull] string value) { }
        [return: MaybeNull] public string NullableResult() => string.Empty;
        [return: NotNull] public string? RequiredResult() => string.Empty;
    }
    private sealed class ValueFlow
    {
        public void WriteRestricted([DisallowNull] Guid? value) { }
        [return: NotNull] public Guid? ResultRequired() => Guid.Empty;
        public Guid? Ordinary(Guid? value) => value;
    }
    private sealed class ValueOnly { public int Read(Guid? value) => 1; }

#nullable disable
    // This fixture exposes oblivious reference metadata rather than nullable or nonnullable annotations.
    private sealed class ObliviousReference { public void Accept(string value) { } }
#nullable restore
}
