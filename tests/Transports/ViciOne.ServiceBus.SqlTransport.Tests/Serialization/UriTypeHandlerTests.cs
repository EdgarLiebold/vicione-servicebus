using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using PostgreSqlUriTypeHandler = ViciOne.ServiceBus.SqlTransport.PostgreSql.UriTypeHandler;
using SqlServerUriTypeHandler = ViciOne.ServiceBus.SqlTransport.SqlServer.UriTypeHandler;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Serialization;

public sealed class UriTypeHandlerTests
{
    [Theory]
    [InlineData(SqlProvider.PostgreSql, "queue:orders")]
    [InlineData(SqlProvider.PostgreSql, "orders/priority")]
    [InlineData(SqlProvider.SqlServer, "https://broker.example/orders")]
    [InlineData(SqlProvider.SqlServer, "orders/priority")]
    [RequirementCoverage("REQ-VSB-SQL-URI-MATERIALIZATION", "valid-absolute-and-relative-values")]
    public void Parse_PreservesValidAbsoluteAndRelativeValues(SqlProvider provider, string value)
    {
        Uri actual = Parse(provider, value);

        Assert.Equal(value, actual.OriginalString);
    }

    [Theory]
    [InlineData(SqlProvider.PostgreSql)]
    [InlineData(SqlProvider.SqlServer)]
    [RequirementCoverage("REQ-VSB-SQL-URI-MATERIALIZATION", "database-null-is-not-a-nonnullable-uri")]
    public void Parse_RejectsNullAndDatabaseNull(SqlProvider provider)
    {
        Assert.Throws<ArgumentNullException>(() => Parse(provider, null!));
        Assert.Throws<InvalidCastException>(() => Parse(provider, DBNull.Value));
    }

    [Theory]
    [InlineData(SqlProvider.PostgreSql, "")]
    [InlineData(SqlProvider.PostgreSql, "   ")]
    [InlineData(SqlProvider.PostgreSql, "http://[")]
    [InlineData(SqlProvider.SqlServer, "")]
    [InlineData(SqlProvider.SqlServer, "   ")]
    [InlineData(SqlProvider.SqlServer, "http://[")]
    [RequirementCoverage("REQ-VSB-SQL-URI-MATERIALIZATION", "empty-whitespace-and-malformed-values")]
    public void Parse_RejectsInvalidText(SqlProvider provider, string value)
    {
        Assert.Throws<UriFormatException>(() => Parse(provider, value));
    }

    [Theory]
    [InlineData(SqlProvider.PostgreSql)]
    [InlineData(SqlProvider.SqlServer)]
    [RequirementCoverage("REQ-VSB-SQL-URI-MATERIALIZATION", "non-string-values-are-rejected")]
    public void Parse_RejectsNonStringValues(SqlProvider provider)
    {
        InvalidCastException exception = Assert.Throws<InvalidCastException>(() => Parse(provider, 42));

        Assert.Contains(typeof(int).FullName!, exception.Message, StringComparison.Ordinal);
    }

    private static Uri Parse(SqlProvider provider, object value) => provider switch
    {
        SqlProvider.PostgreSql => new PostgreSqlUriTypeHandler().Parse(value),
        SqlProvider.SqlServer => new SqlServerUriTypeHandler().Parse(value),
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
    };

    public enum SqlProvider
    {
        PostgreSql,
        SqlServer,
    }
}
