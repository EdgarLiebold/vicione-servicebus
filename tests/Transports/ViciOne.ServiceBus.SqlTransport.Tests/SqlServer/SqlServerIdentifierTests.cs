using ViciOne.ServiceBus.SqlTransport.SqlServer;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.SqlServer;

public sealed class SqlServerIdentifierTests
{
    [Theory]
    [InlineData("42-transport")]
    [InlineData("transport_database")]
    [RequirementCoverage("REQ-VSB-SQLSERVER-DDL-IDENTIFIER", "database-identifiers-preserve-safe-values")]
    public void ValidateDatabase_PreservesSafeBracketDelimitedNames(string identifier)
    {
        Assert.Equal(identifier, SqlServerIdentifier.ValidateDatabase(identifier, "database"));
    }

    [Theory]
    [InlineData("DOMAIN\\transport-user")]
    [InlineData("transport.user@example.com")]
    [InlineData("transport_machine$")]
    [RequirementCoverage("REQ-VSB-SQLSERVER-DDL-IDENTIFIER", "principal-identifiers-preserve-safe-values")]
    public void ValidatePrincipal_PreservesSupportedLoginAndUserNames(string identifier)
    {
        Assert.Equal(identifier, SqlServerIdentifier.ValidatePrincipal(identifier, "principal"));
    }

    [Theory]
    [InlineData("transport")]
    [InlineData("_tenant42")]
    [InlineData("Tenant_42")]
    [RequirementCoverage("REQ-VSB-SQLSERVER-DDL-IDENTIFIER", "regular-identifiers-preserve-safe-values")]
    public void ValidateRegular_PreservesSafeSchemaAndRoleNames(string identifier)
    {
        Assert.Equal(identifier, SqlServerIdentifier.ValidateRegular(identifier, "identifier"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("42tenant")]
    [InlineData("tenant-name")]
    [InlineData("tenant] DROP TABLE Message;--")]
    [InlineData("tenant\nschema")]
    [RequirementCoverage("REQ-VSB-SQLSERVER-DDL-IDENTIFIER", "unsafe-regular-identifiers-are-rejected")]
    public void ValidateRegular_RejectsValuesThatCanEscapeProvisioningTemplates(string? identifier)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            SqlServerIdentifier.ValidateRegular(identifier, "identifier"));

        Assert.Equal("identifier", exception.ParamName);
    }

    [Theory]
    [InlineData("database]")]
    [InlineData("database/name")]
    [InlineData("principal name")]
    [InlineData("principal;")]
    [RequirementCoverage("REQ-VSB-SQLSERVER-DDL-IDENTIFIER", "specialized-identifiers-reject-template-escapes")]
    public void SpecializedValidators_RejectUnsupportedCharacters(string identifier)
    {
        ArgumentException databaseException = Assert.Throws<ArgumentException>(() =>
            SqlServerIdentifier.ValidateDatabase(identifier, "database"));
        ArgumentException principalException = Assert.Throws<ArgumentException>(() =>
            SqlServerIdentifier.ValidatePrincipal(identifier, "principal"));

        Assert.Equal("database", databaseException.ParamName);
        Assert.Equal("principal", principalException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQLSERVER-DDL-IDENTIFIER", "identifier-length-is-bounded")]
    public void ValidateRegular_EnforcesTheSqlServerIdentifierLengthLimit()
    {
        string maximum = "_" + new string('a', 127);
        string tooLong = maximum + "b";

        Assert.Equal(maximum, SqlServerIdentifier.ValidateRegular(maximum, "identifier"));
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            SqlServerIdentifier.ValidateRegular(tooLong, "identifier"));

        Assert.Equal("identifier", exception.ParamName);
        Assert.Contains("128", exception.Message, StringComparison.Ordinal);
    }
}
