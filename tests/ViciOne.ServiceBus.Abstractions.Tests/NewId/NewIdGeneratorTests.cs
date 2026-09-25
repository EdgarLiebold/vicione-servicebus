using System.Data.SqlTypes;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using NewIdValue = global::ViciOne.ServiceBus.Advanced.NewId;

namespace ViciOne.ServiceBus.Abstractions.Tests.NewId;

public sealed class NewIdGeneratorTests
{
    private const long KnownTicks = 8410219332513447152;
    private static readonly byte[] KnownWorkerId = [0x7D, 0x81, 0xBF, 0x34, 0x43, 0x7F, 0x2C, 0x5F];

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "known-guid")]
    public void FixedInputs_ProduceTheKnownGuid()
    {
        var generator = NewIdTestInputs.CreateGenerator(KnownTicks, KnownWorkerId);
        Advance(generator, 267);

        Assert.Equal(Guid.Parse("7d810b01-437f-bf34-3cf0-74b719ec7596"), generator.NextGuid());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "known-sequential-guid")]
    public void FixedInputs_ProduceTheKnownSequentialGuid()
    {
        var generator = NewIdTestInputs.CreateGenerator(KnownTicks, KnownWorkerId);
        Advance(generator, 267);

        Assert.Equal(Guid.Parse("74b719ec-7596-3cf0-bf34-437f7d810b01"), generator.NextSequentialGuid());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "known-sequential-batch")]
    public void SequentialBatch_ContinuesAtTheExactKnownValues()
    {
        var generator = NewIdTestInputs.CreateGenerator(KnownTicks, KnownWorkerId);
        Advance(generator, 267);
        var batch = new Guid[3];

        generator.NextSequentialGuid(batch, 0, batch.Length);

        Assert.Equal(
            [
                Guid.Parse("74b719ec-7596-3cf0-bf34-437f7d810b01"),
                Guid.Parse("74b719ec-7596-3cf0-bf34-437f7d810c01"),
                Guid.Parse("74b719ec-7596-3cf0-bf34-437f7d810d01"),
            ],
            batch);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "guid-call-shapes")]
    public void NewIdAndGuidCallShapes_AdvanceOneSharedSequence()
    {
        var generator = NewIdTestInputs.CreateGenerator();

        var first = generator.Next().ToGuid();
        var second = generator.NextGuid();
        var third = generator.NextGuid();

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, third);
        Assert.True(second.CompareTo(first) > 0);
        Assert.True(third.CompareTo(second) > 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "sequential-guid-call-shapes")]
    public void NewIdAndSequentialGuidCallShapes_AdvanceOneSharedSequence()
    {
        var generator = NewIdTestInputs.CreateGenerator();

        var firstId = generator.Next();
        var first = firstId.ToSequentialGuid();
        var second = generator.NextSequentialGuid();
        var third = generator.NextSequentialGuid();

        Assert.NotEqual(first, second);
        Assert.NotEqual(second, third);
        Assert.True(second.CompareTo(first) > 0);
        Assert.True(third.CompareTo(second) > 0);
        Assert.Equal(firstId, first.ToNewIdFromSequential());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "equal-providers")]
    public void EqualProviders_ProduceEqualFirstValuesAcrossCallShapes()
    {
        var processId = new byte[] { 10, 0, 0, 0 };
        var first = NewIdTestInputs.CreateGenerator(processId: processId);
        var second = NewIdTestInputs.CreateGenerator(processId: processId);

        Assert.Equal(first.Next(), second.Next());
        Assert.Equal(first.Next().ToGuid(), second.NextGuid());
    }

    [Theory]
    [InlineData(null, new byte[] { 10, 0 })]
    [InlineData(new byte[] { 10, 0 }, new byte[] { 11, 0 })]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "process-id-separates-generators")]
    public void DifferentProcessIdentity_ProducesDifferentValues(byte[]? leftProcessId, byte[] rightProcessId)
    {
        var left = NewIdTestInputs.CreateGenerator(processId: leftProcessId).Next();
        var right = NewIdTestInputs.CreateGenerator(processId: rightProcessId).Next();

        Assert.NotEqual(left, right);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "timestamp")]
    public void GeneratedValue_CarriesTheExactTickTimestamp()
    {
        var generator = NewIdTestInputs.CreateGenerator();

        Assert.Equal(NewIdTestInputs.Moment, generator.Next().Timestamp);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "normal-layout")]
    public void NormalGuidLayout_PreservesTickAndAdvancesSequence()
    {
        var generator = NewIdTestInputs.CreateGenerator();
        var first = generator.NextGuid().ToString("D");
        var batch = new Guid[3];
        generator.NextGuid(batch, 0, batch.Length);
        var rendered = batch.Select(value => value.ToString("D")).ToArray();

        Assert.All(rendered, value => Assert.Equal(first[..4], value[..4]));
        Assert.All(rendered, value => Assert.Equal(first[6..], value[6..]));
        Assert.Equal([1, 2, 3], rendered.Select(value => int.Parse(value.Substring(4, 2))).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "guid-batch-rollover")]
    public void GuidBatch_RolloverPreservesIdentityTimestampAndSqlOrder(bool crossesTickWord)
    {
        const int Rollover = 65535;
        var ticks = crossesTickWord
            ? (NewIdTestInputs.Moment.Ticks & ~0xffffffffL) | 0xffffffffL
            : NewIdTestInputs.Moment.Ticks;
        var moment = new DateTime(ticks, DateTimeKind.Utc);
        var generator = NewIdTestInputs.CreateGenerator(ticks);
        var scalar = NewIdTestInputs.CreateGenerator(ticks);
        var sentinel = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ids = new Guid[Rollover + 4];
        ids[0] = sentinel;
        ids[^1] = sentinel;

        var segment = generator.NextGuid(ids, 1, Rollover + 2);

        Assert.Equal(1, segment.Offset);
        Assert.Equal(Rollover + 2, segment.Count);
        Assert.Same(ids, segment.Array);
        Assert.Equal(sentinel, ids[0]);
        Assert.Equal(sentinel, ids[^1]);
        Assert.Equal(Rollover + 2, ids.Skip(1).Take(Rollover + 2).Distinct().Count());
        Assert.Equal(moment, NewIdValue.FromGuid(ids[Rollover]).Timestamp);
        Assert.Equal(moment.AddTicks(1), NewIdValue.FromGuid(ids[Rollover + 1]).Timestamp);
        Assert.Equal(moment.AddTicks(1), NewIdValue.FromGuid(ids[Rollover + 2]).Timestamp);
        Assert.True(new SqlGuid(ids[Rollover]).CompareTo(new SqlGuid(ids[Rollover + 1])) < 0);
        Assert.True(new SqlGuid(ids[Rollover + 1]).CompareTo(new SqlGuid(ids[Rollover + 2])) < 0);

        Advance(scalar, Rollover - 1);
        Assert.Equal(scalar.NextGuid(), ids[Rollover]);
        Assert.Equal(scalar.NextGuid(), ids[Rollover + 1]);
        Assert.Equal(scalar.NextGuid(), ids[Rollover + 2]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "sequential-layout")]
    public void SequentialGuidLayout_PreservesTickAndAdvancesSequence()
    {
        var generator = NewIdTestInputs.CreateGenerator();
        var first = generator.NextSequentialGuid().ToString("D");
        var batch = new Guid[3];
        generator.NextSequentialGuid(batch, 0, batch.Length);
        var rendered = batch.Select(value => value.ToString("D")).ToArray();

        Assert.All(rendered, value => Assert.Equal(first[..14], value[..14]));
        Assert.All(rendered, value => Assert.Equal(first.Substring(19, 13), value.Substring(19, 13)));
        Assert.Equal([1, 2, 3], rendered.Select(value => int.Parse(value.Substring(32, 2))).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "sequential-guid-batch-rollover")]
    public void SequentialGuidBatch_RolloverPreservesIdentityTimestampAndOrder(bool crossesTickWord)
    {
        const int Rollover = 65535;
        var ticks = crossesTickWord
            ? (NewIdTestInputs.Moment.Ticks & ~0xffffffffL) | 0xffffffffL
            : NewIdTestInputs.Moment.Ticks;
        var moment = new DateTime(ticks, DateTimeKind.Utc);
        var generator = NewIdTestInputs.CreateGenerator(ticks);
        var scalar = NewIdTestInputs.CreateGenerator(ticks);
        var sentinel = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var ids = new Guid[Rollover + 4];
        ids[0] = sentinel;
        ids[^1] = sentinel;

        var segment = generator.NextSequentialGuid(ids, 1, Rollover + 2);

        Assert.Equal(1, segment.Offset);
        Assert.Equal(Rollover + 2, segment.Count);
        Assert.Same(ids, segment.Array);
        Assert.Equal(sentinel, ids[0]);
        Assert.Equal(sentinel, ids[^1]);
        Assert.Equal(Rollover + 2, ids.Skip(1).Take(Rollover + 2).Distinct().Count());
        Assert.Equal(moment, NewIdValue.FromSequentialGuid(ids[Rollover]).Timestamp);
        Assert.Equal(moment.AddTicks(1), NewIdValue.FromSequentialGuid(ids[Rollover + 1]).Timestamp);
        Assert.Equal(moment.AddTicks(1), NewIdValue.FromSequentialGuid(ids[Rollover + 2]).Timestamp);
        Assert.True(ids[Rollover].CompareTo(ids[Rollover + 1]) < 0);
        Assert.True(ids[Rollover + 1].CompareTo(ids[Rollover + 2]) < 0);

        for (var index = 0; index < Rollover - 1; index++)
            scalar.NextSequentialGuid();
        Assert.Equal(scalar.NextSequentialGuid(), ids[Rollover]);
        Assert.Equal(scalar.NextSequentialGuid(), ids[Rollover + 1]);
        Assert.Equal(scalar.NextSequentialGuid(), ids[Rollover + 2]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "sequential-uniqueness")]
    public void SequentialGeneration_ProducesNoDuplicates()
    {
        const int Count = 200_000;
        var generator = NewIdTestInputs.CreateGenerator();
        var ids = new NewIdValue[Count];

        generator.Next(ids, 0, ids.Length);

        Assert.Equal(Count, ids.Distinct().Count());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-NEWID-GENERATOR", "parallel-uniqueness")]
    public void ConcurrentGeneration_ProducesNoDuplicates()
    {
        const int Count = 200_000;
        var generator = NewIdTestInputs.CreateGenerator();
        var ids = new NewIdValue[Count];

        Parallel.For(0, Count, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index => ids[index] = generator.Next());

        Assert.Equal(Count, ids.Distinct().Count());
    }

    private static void Advance(NewIdGenerator generator, int count)
    {
        for (var index = 0; index < count; index++)
            generator.NextGuid();
    }
}
