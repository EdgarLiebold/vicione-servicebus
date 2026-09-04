using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using NewIdValue = global::ViciOne.ServiceBus.NewId;

namespace ViciOne.ServiceBus.Abstractions.Tests.NewId;

public sealed class NewIdGuidInteropTests
{
    private static readonly Guid KnownGuid = Guid.Parse("f6b27c7c-8ab8-4498-ac97-3a6107a21320");
    private static readonly byte[] OrderedBytes = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "guid-to-newid")]
    public void GuidConversion_PreservesTheStandardRepresentation()
    {
        Assert.Equal(KnownGuid.ToString("D"), KnownGuid.ToNewId().ToString("D"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "newid-to-guid")]
    public void NewIdConversion_PreservesTheStandardRepresentation()
    {
        var id = new NewIdValue(KnownGuid.ToByteArray());

        Assert.Equal(id.ToString("D"), id.ToGuid().ToString("D"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "guid-extension-round-trip")]
    public void GuidExtensionRoundTrip_ReturnsTheOriginalGuid()
    {
        Assert.Equal(KnownGuid, KnownGuid.ToNewId().ToGuid());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "byte-round-trip")]
    public void ByteRoundTrip_ReturnsTheOriginalGuid()
    {
        var roundTrip = new Guid(new NewIdValue(KnownGuid.ToByteArray()).ToByteArray());

        Assert.Equal(KnownGuid, roundTrip);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "sequential-guid-round-trip")]
    public void SequentialGuidRoundTrip_ReturnsTheOriginalGuid()
    {
        Assert.Equal(KnownGuid, KnownGuid.ToNewIdFromSequential().ToSequentialGuid());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "newid-byte-guid-round-trip")]
    public void NewIdThroughGuidBytes_ReturnsTheOriginalValue()
    {
        var id = new NewIdValue(KnownGuid.ToByteArray());
        var roundTrip = new NewIdValue(new Guid(id.ToByteArray()).ToByteArray());

        Assert.Equal(id, roundTrip);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "guid-constructor-parity")]
    public void ComponentConstructor_MatchesTheGuidConstructor()
    {
        var guid = new Guid(0x01020304, 0x0506, 0x0708, 9, 10, 11, 12, 13, 14, 15, 16);
        var id = new NewIdValue(0x01020304, 0x0506, 0x0708, 9, 10, 11, 12, 13, 14, 15, 16);

        Assert.Equal(guid.ToString("D"), id.ToString("D"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "standard-string-passthrough")]
    public void StandardStringPassthrough_ReturnsTheOriginalValue()
    {
        var id = new NewIdValue(KnownGuid.ToByteArray());
        var guid = new Guid(id.ToString("D"));
        var roundTrip = new NewIdValue(guid.ToString("D"));

        Assert.Equal(id, roundTrip);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "from-guid-preserves-generated-value")]
    public void FromGuid_ReturnsTheGeneratedValueAndReadableTimestamp()
    {
        var id = NewIdTestInputs.CreateGenerator().Next();

        var roundTrip = NewIdValue.FromGuid(id.ToGuid());

        Assert.Equal(id, roundTrip);
        Assert.Equal(NewIdTestInputs.Moment, roundTrip.Timestamp);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "from-sequential-guid-preserves-generated-value")]
    public void FromSequentialGuid_ReturnsTheGeneratedValueAndReadableTimestamp()
    {
        var id = NewIdTestInputs.CreateGenerator().Next();

        var roundTrip = NewIdValue.FromSequentialGuid(id.ToSequentialGuid());

        Assert.Equal(id, roundTrip);
        Assert.Equal(NewIdTestInputs.Moment, roundTrip.Timestamp);
    }

    [Theory]
    [InlineData("B")]
    [InlineData("D")]
    [InlineData("N")]
    [InlineData("P")]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "standard-format-parity")]
    public void StandardFormats_MatchGuid(string format)
    {
        var guid = new Guid(OrderedBytes);
        var id = new NewIdValue(OrderedBytes);

        Assert.Equal(guid.ToString(format), id.ToString(format));
    }

    [Theory]
    [InlineData("Bs")]
    [InlineData("Ds")]
    [InlineData("Ns")]
    [InlineData("Ps")]
    [RequirementCoverage("REQ-VSB-NEWID-GUID-INTEROP", "sequential-format-parity")]
    public void SequentialFormats_ParseAsTheSequentialGuid(string format)
    {
        var id = new NewIdValue(OrderedBytes);

        var parsed = Guid.Parse(id.ToString(format));

        Assert.Equal(id.ToSequentialGuid(), parsed);
        if (string.Equals(format, "Ds", StringComparison.Ordinal))
            Assert.NotEqual(id.ToGuid(), parsed);
    }
}
