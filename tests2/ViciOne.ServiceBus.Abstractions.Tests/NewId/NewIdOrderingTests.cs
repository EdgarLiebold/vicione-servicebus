namespace ViciOne.ServiceBus.Abstractions.Tests.NewId;

using System.Data.SqlTypes;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class NewIdOrderingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-ORDERING", "batch-sql-server")]
    public void Batch_IsStrictlyIncreasingInSqlServerGuidOrder()
    {
        var generator = NewIdTestInputs.CreateGenerator();
        generator.Next();
        var ids = new global::ViciOne.ServiceBus.NewId[1024];

        generator.Next(ids, 0, ids.Length);

        for (var index = 0; index < ids.Length - 1; index++)
        {
            Assert.NotEqual(ids[index], ids[index + 1]);
            Assert.True(new SqlGuid(ids[index].ToGuid()).CompareTo(new SqlGuid(ids[index + 1].ToGuid())) < 0);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-ORDERING", "batch-sequential-guid")]
    public void Batch_IsStrictlyIncreasingInSequentialGuidOrder()
    {
        var generator = NewIdTestInputs.CreateGenerator();
        generator.Next();
        var ids = new global::ViciOne.ServiceBus.NewId[1024];

        generator.Next(ids, 0, ids.Length);

        for (var index = 0; index < ids.Length - 1; index++)
        {
            Assert.NotEqual(ids[index], ids[index + 1]);
            Assert.True(ids[index].ToSequentialGuid().CompareTo(ids[index + 1].ToSequentialGuid()) < 0);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-ORDERING", "advancing-ticks-sql-server")]
    public void IncreasingTickIntervals_RemainStrictlyIncreasingInSqlServerGuidOrder()
    {
        var tickProvider = new NewIdTestInputs.AdvancingTickProvider(
            NewIdTestInputs.Moment.Ticks,
            TimeSpan.FromSeconds(2).Ticks,
            TimeSpan.FromSeconds(30).Ticks);
        var generator = new NewIdGenerator(tickProvider, new NewIdTestInputs.FixedWorkerIdProvider([1, 2, 3, 4, 5, 6]));
        var ids = new global::ViciOne.ServiceBus.NewId[1024];

        generator.Next(ids, 0, ids.Length);

        for (var index = 0; index < ids.Length - 1; index++)
            Assert.True(new SqlGuid(ids[index].ToGuid()).CompareTo(new SqlGuid(ids[index + 1].ToGuid())) < 0);
    }
}
