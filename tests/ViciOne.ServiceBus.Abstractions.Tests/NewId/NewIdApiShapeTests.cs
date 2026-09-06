using System.Reflection;
using ViciOne.ServiceBus.NewIdFormatters;
using ViciOne.ServiceBus.NewIdParsers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using NewIdValue = global::ViciOne.ServiceBus.Advanced.NewId;

namespace ViciOne.ServiceBus.Abstractions.Tests.NewId;

public sealed class NewIdApiShapeTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-SPAN-API", "read-only-span-contract")]
    public void PublicIdentifierByteAndTextContracts_UseReadOnlySpans()
    {
        ConstructorInfo bytesConstructor = Assert.Single(typeof(NewIdValue).GetConstructors(), constructor =>
            constructor.GetParameters() is [{ ParameterType: var type }] && type == typeof(ReadOnlySpan<byte>));
        Assert.Equal("bytes", Assert.Single(bytesConstructor.GetParameters()).Name);

        MethodInfo format = Assert.Single(typeof(INewIdFormatter).GetMethods());
        Assert.Equal(typeof(ReadOnlySpan<byte>), Assert.Single(format.GetParameters()).ParameterType);

        MethodInfo parse = Assert.Single(typeof(INewIdParser).GetMethods());
        Assert.Equal(typeof(ReadOnlySpan<char>), Assert.Single(parse.GetParameters()).ParameterType);

        Type[] identifierTypes =
        [
            typeof(NewIdValue),
            typeof(INewIdFormatter),
            typeof(INewIdParser),
            typeof(Base32Formatter),
            typeof(DashedHexFormatter),
            typeof(HexFormatter),
            typeof(ZBase32Formatter),
            typeof(Base32Parser),
            typeof(ZBase32Parser),
        ];
        Assert.DoesNotContain(
            identifierTypes.SelectMany(PublicSignatures),
            parameter => parameter.ParameterType.IsByRef
                && parameter.ParameterType.GetElementType() is { } element
                && (element == typeof(string) || element == typeof(byte[])));
    }

    [Theory]
    [MemberData(nameof(Formatters))]
    [RequirementCoverage("REQ-VSB-NEWID-SPAN-API", "exact-sixteen-byte-boundary")]
    public void Formatter_RequiresExactlySixteenBytes(INewIdFormatter formatter)
    {
        ArgumentException shortInput = Assert.Throws<ArgumentException>(() => formatter.Format(new byte[15]));
        ArgumentException longInput = Assert.Throws<ArgumentException>(() => formatter.Format(new byte[17]));

        Assert.Equal("bytes", shortInput.ParamName);
        Assert.Equal("bytes", longInput.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-SPAN-API", "constructor-input-validation")]
    public void CustomAlphabetConstructors_RejectNull()
    {
        Assert.Equal("chars", Assert.Throws<ArgumentNullException>(() => new Base32Formatter(null!)).ParamName);
        Assert.Equal("chars", Assert.Throws<ArgumentNullException>(() => new Base32Parser(null!)).ParamName);
    }

    public static TheoryData<INewIdFormatter> Formatters() =>
    [
        new Base32Formatter(),
        new DashedHexFormatter(),
        new HexFormatter(),
        new ZBase32Formatter(),
    ];

    private static IEnumerable<ParameterInfo> PublicSignatures(Type type)
    {
        const BindingFlags members = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return type.GetConstructors(members).SelectMany(static constructor => constructor.GetParameters())
            .Concat(type.GetMethods(members).SelectMany(static method => method.GetParameters()));
    }
}
