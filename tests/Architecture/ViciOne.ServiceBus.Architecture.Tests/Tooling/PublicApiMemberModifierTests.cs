using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using ViciOne.ServiceBus.Build;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Tooling;

public sealed class PublicApiMemberModifierTests
{
    [Theory]
    [InlineData(nameof(Parameters.Token), "System.Threading.CancellationToken")]
    [InlineData(nameof(Parameters.Identifier), "System.Guid")]
    [InlineData(nameof(Parameters.Timestamp), "System.DateTime")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-defaults-preserve-typed-value-defaults")]
    public void ParameterDefaults_PreserveTypedValueDefaults(string method, string type)
        => Assert.Equal($"METHOD public static System.Void {method}({type} value = default({type}))", Method(typeof(Parameters), method));

    [Theory]
    [InlineData(nameof(Parameters.NullableIdentifier), "System.Nullable<System.Guid>")]
    [InlineData(nameof(Parameters.NullableText), "System.String")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-defaults-keep-nullable-and-reference-null")]
    public void ParameterDefaults_KeepNullableAndReferenceNullDefaults(string method, string type)
        => Assert.Equal($"METHOD public static System.Void {method}({type} value = null)", Method(typeof(Parameters), method));

    [Theory]
    [InlineData(nameof(Parameters.Text), "System.String value = \"a\\\"b\\\\c\\n\"")]
    [InlineData(nameof(Parameters.Apostrophe), "System.Char value = '\\''")]
    [InlineData(nameof(Parameters.Newline), "System.Char value = '\\n'")]
    [InlineData(nameof(Parameters.Control), "System.Char value = '\\u0001'")]
    [InlineData(nameof(Parameters.Amount), "System.Decimal value = 1.25")]
    [InlineData(nameof(Parameters.Enabled), "System.Boolean value = true")]
    [InlineData(nameof(Parameters.Mode), "System.StringComparison value = 4")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-defaults-preserve-escaped-invariant-literals")]
    public void ParameterDefaults_EscapeLiteralsAndFormatConstantsInvariantly(string method, string parameter)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal($"METHOD public static System.Void {method}({parameter})", Method(typeof(Parameters), method));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-optionality-preserves-optional-without-default")]
    public void ParameterOptionality_PreserveOptionalWithoutDefault()
    {
        ParameterInfo parameter = typeof(Parameters).GetMethod(nameof(Parameters.Optional))!.GetParameters()[0];
        Assert.True(parameter.IsOptional);
        Assert.False(parameter.HasDefaultValue);
        Assert.Equal("METHOD public static System.Void Optional(System.Int32 value [optional])", Method(typeof(Parameters), nameof(Parameters.Optional)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-optionality-distinguishes-required-parameters")]
    public void ParameterOptionality_DistinguishOptionalFromRequired()
    {
        string required = Method(typeof(Required), nameof(Required.Accept));
        string optional = Method(typeof(Optional), nameof(Optional.Accept));
        Assert.Equal("METHOD public static System.Void Accept(System.Int32 value)", required);
        Assert.Equal("METHOD public static System.Void Accept(System.Int32 value [optional])", optional);
        Assert.NotEqual(required, optional);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-optionality-keeps-default-constant-independent-of-optional-flag")]
    public void ParameterOptionality_KeepDefaultConstantIndependentOfOptionalFlag()
    {
        ParameterInfo parameter = typeof(Parameters).GetMethod(nameof(Parameters.RequiredConstant))!.GetParameters()[0];
        Assert.False(parameter.IsOptional);
        Assert.True(parameter.HasDefaultValue);
        Assert.Equal(5, parameter.RawDefaultValue);
        Assert.Equal("METHOD public static System.Void RequiredConstant(System.Int32 value = 5 [required])", Method(typeof(Parameters), nameof(Parameters.RequiredConstant)));
    }

    [Theory]
    [InlineData(nameof(Parameters.ByReference), "ref", false, false)]
    [InlineData(nameof(Parameters.ReadOnlyInput), "in", true, false)]
    [InlineData(nameof(Parameters.ReadOnlyLocation), "ref readonly", true, false)]
    [InlineData(nameof(Parameters.Output), "out", false, true)]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-parameters-distinguish-ref-in-ref-readonly-and-out")]
    public void ParameterModifiers_DistinguishRefInReadonlyAndOut(string method, string modifier, bool input, bool output)
    {
        ParameterInfo parameter = typeof(Parameters).GetMethod(method)!.GetParameters()[0];
        Assert.True(parameter.ParameterType.IsByRef);
        Assert.Equal(input, parameter.IsIn);
        Assert.Equal(output, parameter.IsOut);
        Assert.Equal($"METHOD public static System.Void {method}({modifier} System.Int32 value)", Method(typeof(Parameters), method));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-parameters-preserve-virtual-readonly-location-contract")]
    public void ParameterModifiers_PreserveVirtualReadonlyLocationContract()
    {
        ParameterInfo parameter = typeof(VirtualLocation).GetMethod(nameof(VirtualLocation.Read))!.GetParameters()[0];
        Assert.Equal(["System.Runtime.InteropServices.InAttribute"], parameter.GetRequiredCustomModifiers().Select(type => type.FullName));
        Assert.Empty(parameter.GetOptionalCustomModifiers());
        Assert.Contains(parameter.GetCustomAttributesData(), attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.RequiresLocationAttribute");
        Assert.Equal("METHOD public virtual System.Int32 Read(ref readonly System.Int32 value [modreq=[System.Runtime.InteropServices.InAttribute];modopt=[]])", Method(typeof(VirtualLocation), nameof(VirtualLocation.Read)));
    }

    [Theory]
    [InlineData(nameof(Parameters.InputBuffer), "System.Byte[]", "", true, false, false)]
    [InlineData(nameof(Parameters.OutputBuffer), "System.Byte[]", "", false, true, false)]
    [InlineData(nameof(Parameters.BidirectionalBuffer), "System.Byte[]", "", true, true, false)]
    [InlineData(nameof(Parameters.InputReference), "System.Int32", "ref ", true, false, true)]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-direction-flags-do-not-invent-byref-or-readonly-contracts")]
    public void ParameterDirections_DoNotInventByReferenceOrReadonlyContracts(string method, string type, string modifier, bool input, bool output, bool byReference)
    {
        ParameterInfo parameter = typeof(Parameters).GetMethod(method)!.GetParameters()[0];
        Assert.Equal(byReference, parameter.ParameterType.IsByRef);
        Assert.Equal(input, parameter.IsIn);
        Assert.Equal(output, parameter.IsOut);
        string direction = input && output ? "in&out" : input ? "in" : "out";
        Assert.Equal($"METHOD public static System.Void {method}({modifier}{type} value [direction={direction}])", Method(typeof(Parameters), method));
    }

    [Theory]
    [InlineData(nameof(Parameters.DefaultStruct))]
    [InlineData(nameof(Parameters.DefaultUnmanaged))]
    [InlineData(nameof(Parameters.DefaultUnconstrained))]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-defaults-preserve-typed-generic-defaults")]
    public void ParameterDefaults_PreserveTypedGenericDefaults(string method)
    {
        ParameterInfo parameter = typeof(Parameters).GetMethod(method)!.GetParameters()[0];
        Assert.True(parameter.ParameterType.IsGenericParameter);
        Assert.True(parameter.HasDefaultValue);
        Assert.Null(parameter.RawDefaultValue);
        Assert.StartsWith($"METHOD public static System.Void {method}<T>(T value = default(T)) [generic=", Method(typeof(Parameters), method), StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-constructors-share-typed-default-contract")]
    public void ConstructorDefaults_PreserveTypedOptionalValueDefaults()
        => Assert.Equal(
            "CTOR public ViciOne.ServiceBus.Architecture.Tests.Tooling.PublicApiMemberModifierTests.Constructed(System.Threading.CancellationToken value = default(System.Threading.CancellationToken))",
            Assert.Single(PublicApiBaseline.FormatMembers(typeof(Constructed)), row => row.StartsWith("CTOR ", StringComparison.Ordinal)));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-literals-keep-character-and-string-delimiters-distinct")]
    public void LiteralFields_KeepCharacterAndStringDelimitersDistinct()
        => Assert.Equal(
            ["FIELD public const System.Char Apostrophe = '\\''", "FIELD public const System.String Text = \"it's\""],
            PublicApiBaseline.FormatMembers(typeof(Literals)).Where(row => row.StartsWith("FIELD ", StringComparison.Ordinal)).Order(StringComparer.Ordinal));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-parameters-preserve-params-array-and-collection")]
    public void ParameterArrays_PreserveParamsApartFromOrdinaryArrays()
    {
        Assert.Equal("METHOD public static System.Void Array(System.Int32[] values)", Method(typeof(Parameters), nameof(Parameters.Array)));
        Assert.Equal("METHOD public static System.Void ArrayParams(params System.Int32[] values)", Method(typeof(Parameters), nameof(Parameters.ArrayParams)));
        Assert.Equal("METHOD public static System.Void CollectionParams(params System.Collections.Generic.IEnumerable<System.Int32> values)", Method(typeof(Parameters), nameof(Parameters.CollectionParams)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-accessors-distinguish-init-from-set")]
    public void PropertyAccessors_DistinguishInitFromSet()
    {
        string set = Property(typeof(Settable));
        string init = Property(typeof(Initializable));
        Assert.Equal("PROPERTY System.Int32 Value { public-get; public-set; }", set);
        Assert.Equal("PROPERTY System.Int32 Value { public-get; public-init[return-modreq=[System.Runtime.CompilerServices.IsExternalInit];return-modopt=[]]; }", init);
        Assert.NotEqual(set, init);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-return-modifiers-distinguish-readonly-references")]
    public void ReturnModifiers_DistinguishReadonlyFromWritableReferences()
    {
        string writable = Method(typeof(WritableReference), nameof(WritableReference.Borrow));
        string readOnly = Method(typeof(ReadOnlyReference), nameof(ReadOnlyReference.Borrow));
        Assert.Equal("METHOD public System.Int32& Borrow()", writable);
        Assert.Equal("METHOD public System.Int32& Borrow() [return-modreq=[System.Runtime.InteropServices.InAttribute];return-modopt=[]]", readOnly);
        Assert.NotEqual(writable, readOnly);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-field-modifiers-preserve-volatile-contracts")]
    public void FieldModifiers_PreserveVolatileFieldContracts()
    {
        Assert.Equal("FIELD public System.Int32 Value", Assert.Single(PublicApiBaseline.FormatMembers(typeof(OrdinaryField)), row => row.StartsWith("FIELD ", StringComparison.Ordinal)));
        Assert.Equal("FIELD public System.Int32 Value [modreq=[System.Runtime.CompilerServices.IsVolatile];modopt=[]]", Assert.Single(PublicApiBaseline.FormatMembers(typeof(VolatileField)), row => row.StartsWith("FIELD ", StringComparison.Ordinal)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-method-modifiers-preserve-ordinary-and-sealed-overrides")]
    public void MethodModifiers_DistinguishOrdinaryAndSealedOverrides()
    {
        Assert.Equal("METHOD public virtual System.Int32 Read()", Method(typeof(VirtualBase), nameof(VirtualBase.Read)));
        Assert.Equal("METHOD public override System.Int32 Read()", Method(typeof(OrdinaryOverride), nameof(OrdinaryOverride.Read)));
        Assert.Equal("METHOD public sealed override System.Int32 Read()", Method(typeof(SealedOverride), nameof(SealedOverride.Read)));
    }

    [Theory]
    [InlineData(typeof(StaticProperty), "static")]
    [InlineData(typeof(VirtualProperty), "virtual")]
    [InlineData(typeof(AbstractProperty), "abstract")]
    [InlineData(typeof(OverrideProperty), "override")]
    [InlineData(typeof(SealedOverrideProperty), "sealed override")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-accessors-preserve-static-virtual-and-abstract-modifiers")]
    public void AccessorModifiers_PreserveStaticVirtualAndAbstractContracts(Type type, string modifier)
        => Assert.Equal($"PROPERTY System.Int32 Value {{ public-get[modifiers={modifier}];  }}", Property(type));

    [Theory]
    [InlineData(typeof(OrdinaryEvent), "")]
    [InlineData(typeof(StaticEvent), "[modifiers=static]")]
    [InlineData(typeof(VirtualEvent), "[modifiers=virtual]")]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-events-preserve-each-accessor-contract")]
    public void EventAccessors_PreserveEachAccessorContract(Type type, string modifier)
        => Assert.Equal($"EVENT System.Action Changed {{ public-add{modifier}; public-remove{modifier}; }}", Assert.Single(PublicApiBaseline.FormatMembers(type), row => row.StartsWith("EVENT ", StringComparison.Ordinal)));

    [Fact]
    [RequirementCoverage("REQ-VSB-PACKED-PUBLIC-API", "member-indexers-share-typed-optional-default-contract")]
    public void IndexerDefaults_PreserveTypedOptionalValueDefaults()
        => Assert.Equal("PROPERTY System.Int32 Item[System.Int32 index, System.Guid value = default(System.Guid)] { public-get;  }", Property(typeof(Indexed)));

    private static string Method(Type type, string name) => Assert.Single(PublicApiBaseline.FormatMembers(type), row => row.StartsWith("METHOD ", StringComparison.Ordinal)
        && (row.Contains($" {name}(", StringComparison.Ordinal) || row.Contains($" {name}<", StringComparison.Ordinal)));
    private static string Property(Type type) => Assert.Single(PublicApiBaseline.FormatMembers(type), row => row.StartsWith("PROPERTY ", StringComparison.Ordinal));

    private static class Parameters
    {
        public static void Token(CancellationToken value = default) { }
        public static void Identifier(Guid value = default) { }
        public static void Timestamp(DateTime value = default) { }
        public static void NullableIdentifier(Guid? value = null) { }
        public static void NullableText(string? value = null) { }
        public static void Text(string value = "a\"b\\c\n") { }
        public static void Apostrophe(char value = '\'') { }
        public static void Newline(char value = '\n') { }
        public static void Control(char value = '\u0001') { }
        public static void Amount(decimal value = 1.25m) { }
        public static void Enabled(bool value = true) { }
        public static void Mode(StringComparison value = StringComparison.Ordinal) { }
        public static void Optional([OptionalAttribute] int value) { }
        public static void Array(int[] values) { }
        public static void ArrayParams(params int[] values) { }
        public static void CollectionParams(params IEnumerable<int> values) { }
        public static void RequiredConstant([DefaultParameterValue(5)] int value) { }
        public static void ByReference(ref int value) { }
        public static void ReadOnlyInput(in int value) { }
        public static void ReadOnlyLocation(ref readonly int value) { }
        public static void Output(out int value) => value = 0;
        public static void InputBuffer([In] byte[] value) { }
        public static void OutputBuffer([Out] byte[] value) { }
        public static void BidirectionalBuffer([In, Out] byte[] value) { }
        public static void InputReference([In] ref int value) { }
        public static void DefaultStruct<T>(T value = default) where T : struct { }
        public static void DefaultUnmanaged<T>(T value = default) where T : unmanaged { }
        public static void DefaultUnconstrained<T>(T? value = default) { }
    }

    private static class Required { public static void Accept(int value) { } }
    private static class Optional { public static void Accept([OptionalAttribute] int value) { } }
    private sealed class Settable { public int Value { get; set; } }
    private sealed class Initializable { public int Value { get; init; } }
    private sealed class WritableReference { private int _value; public ref int Borrow() => ref _value; }
    private sealed class ReadOnlyReference { private readonly int _value = 0; public ref readonly int Borrow() => ref _value; }
    private sealed class OrdinaryField { public int Value = 0; }
    private sealed class VolatileField { public volatile int Value = 0; }
    private class VirtualBase { public virtual int Read() => 1; }
    private sealed class OrdinaryOverride : VirtualBase { public override int Read() => 2; }
    private sealed class SealedOverride : VirtualBase { public sealed override int Read() => 3; }
    private sealed class StaticProperty { public static int Value => 1; }
    private class VirtualProperty { public virtual int Value => 1; }
    private abstract class AbstractProperty { public abstract int Value { get; } }
    private sealed class OverrideProperty : VirtualProperty { public override int Value => 2; }
    private sealed class SealedOverrideProperty : VirtualProperty { public sealed override int Value => 3; }
    private class VirtualLocation { public virtual int Read(ref readonly int value) => value; }
    private sealed class Constructed { public Constructed(CancellationToken value = default) { } }
    private static class Literals { public const char Apostrophe = '\''; public const string Text = "it's"; }
    private sealed class OrdinaryEvent { public event Action? Changed { add { } remove { } } }
    private static class StaticEvent { public static event Action? Changed { add { } remove { } } }
    private class VirtualEvent { public virtual event Action? Changed { add { } remove { } } }
    private sealed class Indexed { public int this[int index, Guid value = default] => index; }
}
