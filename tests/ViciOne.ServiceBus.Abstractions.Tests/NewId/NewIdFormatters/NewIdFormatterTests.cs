using System.Text.Json;
using ViciOne.ServiceBus.NewIdFormatters;
using ViciOne.ServiceBus.NewIdParsers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using NewIdValue = global::ViciOne.ServiceBus.Advanced.NewId;

namespace ViciOne.ServiceBus.Abstractions.Tests.NewId.NewIdFormatters;

public sealed class NewIdFormatterTests
{
    private const string ResourceName =
        "ViciOne.ServiceBus.Abstractions.Tests.NewId.NewIdFormatters.ReferenceCorpus.json";

    [Theory]
    [InlineData("Base32Lower")]
    [InlineData("Base32Upper")]
    [InlineData("CustomBase32")]
    [InlineData("DashedHexBase16BracketsLower")]
    [InlineData("DashedHexBase16BracketsUpper")]
    [InlineData("DashedHexBase16Lower")]
    [InlineData("DashedHexBase16Upper")]
    [InlineData("HexBase16Lower")]
    [InlineData("HexBase16Upper")]
    [InlineData("ZBase32Lower")]
    [InlineData("ZBase32Upper")]
    [RequirementCoverage("REQ-VSB-NEWID-FORMATTER", "reference-corpus")]
    public void Formatter_ReproducesEveryReferenceCorpusValue(string encoding)
    {
        var corpus = LoadCorpus();
        var guids = corpus["Guids"];
        var expected = corpus[encoding];
        Assert.Equal(guids.Length, expected.Length);
        var formatter = CreateFormatter(encoding);

        for (var index = 0; index < guids.Length; index++)
            Assert.Equal(expected[index], new NewIdValue(guids[index]).ToString(formatter));
    }

    [Theory]
    [InlineData("Base32", "62ZHY7EKXBCJRLEXHJQQPIQTBA")]
    [InlineData("CustomBase32", "UQP7OV4AN129HB4N79GGF8GJ10")]
    [InlineData("ZBase32", "6438A9RKZBNJTMRZ8JOOXEOUBY")]
    [RequirementCoverage("REQ-VSB-NEWID-FORMATTER", "fixed-expected-output")]
    public void FixedIdentifier_RendersTheExactExpectedText(string formatterName, string expected)
    {
        var id = new NewIdValue("F6B27C7C-8AB8-4498-AC97-3A6107A21320");
        INewIdFormatter formatter = formatterName switch
        {
            "Base32" => new Base32Formatter(true),
            "CustomBase32" => new Base32Formatter("0123456789ABCDEFGHIJKLMNOPQRSTUV"),
            "ZBase32" => new ZBase32Formatter(true),
            _ => throw new ArgumentOutOfRangeException(nameof(formatterName), formatterName, "Unknown formatter"),
        };

        Assert.Equal(expected, id.ToString(formatter));
    }

    [Theory]
    [InlineData("Base32")]
    [InlineData("ZBase32")]
    [RequirementCoverage("REQ-VSB-NEWID-PARSER", "formatter-round-trip")]
    public void Parser_RoundTripsItsFormatter(string encoding)
    {
        var id = new NewIdValue("F6B27C7C-8AB8-4498-AC97-3A6107A21320");

        var roundTrip = encoding switch
        {
            "Base32" => new Base32Parser().Parse(id.ToString(new Base32Formatter(true))),
            "ZBase32" => new ZBase32Parser().Parse(id.ToString(new ZBase32Formatter(true))),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "Unknown parser"),
        };

        Assert.Equal(id, roundTrip);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-PARSER", "human-readable-aliases")]
    public void HumanReadableParser_AcceptsTheDocumentedConfusableCharacters()
    {
        var expected = new NewIdValue("F6B27C7C-8AB8-4498-AC97-3A6107A21320");

        var parsed = new ZBase32Parser(true).Parse("6438A9RK2BNJTMRZ8J0OXE0UBY");

        Assert.Equal(expected, parsed);
    }

    private static INewIdFormatter CreateFormatter(string encoding) => encoding switch
    {
        "Base32Lower" => new Base32Formatter(),
        "Base32Upper" => new Base32Formatter(true),
        "CustomBase32" => new Base32Formatter("0123456789ABCDEFGHIJKLMNOPQRSTUV"),
        "DashedHexBase16BracketsLower" => new DashedHexFormatter('{', '}'),
        "DashedHexBase16BracketsUpper" => new DashedHexFormatter('{', '}', true),
        "DashedHexBase16Lower" => new DashedHexFormatter(),
        "DashedHexBase16Upper" => new DashedHexFormatter(upperCase: true),
        "HexBase16Lower" => new HexFormatter(),
        "HexBase16Upper" => new HexFormatter(true),
        "ZBase32Lower" => new ZBase32Formatter(),
        "ZBase32Upper" => new ZBase32Formatter(true),
        _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "Unknown formatter"),
    };

    private static IReadOnlyDictionary<string, string[]> LoadCorpus()
    {
        var assembly = typeof(NewIdFormatterTests).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException($"Embedded resource '{ResourceName}' was not found.");

        return JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream)
            ?? throw new InvalidDataException($"Embedded resource '{ResourceName}' contains JSON null.");
    }
}
