using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlSendContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-SEND-CONTEXT", "priority-contract-validates-and-preserves-default")]
    public void PriorityExtensions_ValidateTheContextAndPreserveTheTransportDefault()
    {
        var sqlContext = new SqlMessageSendContext<Message>(new Message(), CancellationToken.None);

        sqlContext.SetPriority(7);
        Assert.Equal((short)7, sqlContext.Priority);

        Assert.True(sqlContext.TrySetPriority(100));
        Assert.Null(sqlContext.Priority);

        SendContext nonSqlContext = new MessageSendContext<Message>(new Message());
        Assert.False(nonSqlContext.TrySetPriority(7));
        Assert.Throws<InvalidOperationException>(() => nonSqlContext.SetPriority(7));
        Assert.Throws<ArgumentNullException>(() => SqlSendContextExtensions.SetPriority(null!, 7));
        Assert.Throws<ArgumentNullException>(() => SqlSendContextExtensions.TrySetPriority(null!, 7));
    }

    private sealed record Message;
}
