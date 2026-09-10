using System.Text;
using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.PostgreSql;

public sealed class PostgreSqlIdentifierTests
{
    [Theory]
    [InlineData("transport")]
    [InlineData("tenant-data")]
    [InlineData("tenant.data")]
    [InlineData("München")]
    [RequirementCoverage("REQ-VSB-POSTGRES-DDL-IDENTIFIER", "safe-values-are-preserved")]
    public void Validate_PreservesSafeIdentifiers(string identifier)
    {
        string actual = PostgreSqlIdentifier.Validate(identifier, "identifier");

        Assert.Equal(identifier, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("tenant\"schema")]
    [InlineData("tenant'schema")]
    [InlineData("tenant\nschema")]
    [RequirementCoverage("REQ-VSB-POSTGRES-DDL-IDENTIFIER", "empty-quoted-and-control-values-are-rejected")]
    public void Validate_RejectsValuesThatCanEscapeMigratorTemplates(string? identifier)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() => PostgreSqlIdentifier.Validate(identifier, "identifier"));

        Assert.Equal("identifier", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-DDL-IDENTIFIER", "postgresql-utf8-byte-limit")]
    public void Validate_EnforcesThePostgreSqlUtf8ByteLimit()
    {
        string sixtyThreeBytes = new string('a', 61) + "ä";
        string sixtyFourBytes = new('ä', 32);

        Assert.Equal(63, Encoding.UTF8.GetByteCount(sixtyThreeBytes));
        Assert.Equal(sixtyThreeBytes, PostgreSqlIdentifier.Validate(sixtyThreeBytes, "identifier"));

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => PostgreSqlIdentifier.Validate(sixtyFourBytes, "identifier"));

        Assert.Equal("identifier", exception.ParamName);
        Assert.Contains("63 UTF-8 bytes", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-DDL-IDENTIFIER", "malformed-unicode-is-rejected")]
    public void Validate_RejectsMalformedUnicode()
    {
        const string malformedIdentifier = "tenant\ud800";

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => PostgreSqlIdentifier.Validate(malformedIdentifier, "identifier"));

        Assert.Equal("identifier", exception.ParamName);
        Assert.IsType<EncoderFallbackException>(exception.InnerException);
    }
}
