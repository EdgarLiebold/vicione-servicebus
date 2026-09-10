using ViciOne.ServiceBus.SqlTransport.PostgreSql;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.PostgreSql;

public sealed class PostgreSqlCommandContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-POSTGRES-ROUTINE-NAMES", "client-targets-only-unversioned-routines")]
    public void ClientCommands_TargetOnlyUnversionedTransportRoutines()
    {
        string[] commands =
        [
            PostgreSqlStatements.DbCreateQueueSql,
            PostgreSqlStatements.DbEnqueueSql,
            PostgreSqlStatements.DbPublishSql,
        ];

        Assert.Contains(".create_queue(", commands[0], StringComparison.Ordinal);
        Assert.Contains(".send_message(", commands[1], StringComparison.Ordinal);
        Assert.Contains(".publish_message(", commands[2], StringComparison.Ordinal);
        Assert.All(commands, command => Assert.DoesNotContain("_v2", command, StringComparison.OrdinalIgnoreCase));
    }
}
