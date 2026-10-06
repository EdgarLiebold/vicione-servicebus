using System.Reflection;
using System.Reflection.Emit;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests;

public sealed class AbstractionBoundaryOracleTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-ADMISSION", "default-results-never-claim-committed-or-applied-work")]
    public void DefaultDurableResults_DoNotReportCommittedOrAppliedWork(int route)
    {
        var id = new DurableSendId(Guid.Parse("77777777-1111-2222-3333-444444444444"));
        if (route == 0)
        {
            foreach (DurableSendAdmissionDisposition disposition in Enum.GetValues<DurableSendAdmissionDisposition>())
            {
                var receipt = new DurableSendReceipt(id, disposition, 2, 37);
                Assert.Equal(id, receipt.Id);
                Assert.Equal(2, receipt.StoredCount);
                Assert.Equal(37, receipt.StoredBytes);
                Assert.Equal(disposition == DurableSendAdmissionDisposition.Accepted, receipt.IsNew);
            }
            Assert.Equal("id", Assert.Throws<ArgumentException>(() =>
                new DurableSendReceipt(default, DurableSendAdmissionDisposition.Accepted, 0, 0)).ParamName);
            Assert.Equal(Guid.Empty, default(DurableSendReceipt).Id.Value);
            Assert.False(default(DurableSendReceipt).IsNew);
        }
        else if (route == 1)
        {
            foreach (DurableSendAdmissionDisposition disposition in Enum.GetValues<DurableSendAdmissionDisposition>())
            {
                var result = new DurableSendAdmissionResult(id, disposition, 2, 37);
                Assert.Equal(id, result.Id);
                Assert.Equal(2, result.StoredCount);
                Assert.Equal(37, result.StoredBytes);
                Assert.Equal(disposition == DurableSendAdmissionDisposition.Accepted, result.IsNew);
            }
            Assert.Equal("id", Assert.Throws<ArgumentException>(() =>
                new DurableSendAdmissionResult(default, DurableSendAdmissionDisposition.Accepted, 0, 0)).ParamName);
            Assert.Equal(Guid.Empty, default(DurableSendAdmissionResult).Id.Value);
            Assert.False(default(DurableSendAdmissionResult).IsNew);
        }
        else
        {
            foreach (DurableSendOperationOutcome outcome in Enum.GetValues<DurableSendOperationOutcome>())
            {
                var result = new DurableSendOperationResult(id, outcome);
                Assert.Equal(id, result.Id);
                Assert.Equal(outcome, result.Outcome);
                Assert.Equal(outcome is DurableSendOperationOutcome.Requeued or DurableSendOperationOutcome.Discarded,
                    result.IsApplied);
            }
            Assert.Equal("id", Assert.Throws<ArgumentException>(() =>
                new DurableSendOperationResult(default, DurableSendOperationOutcome.Requeued)).ParamName);
            Assert.Equal(Guid.Empty, default(DurableSendOperationResult).Id.Value);
            Assert.False(default(DurableSendOperationResult).IsApplied);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-LAMBDA-EQUALITY", "null-reflexivity-and-real-set-membership")]
    public void LambdaEquality_PreservesNullReflexivityAndCollectionDeduplication()
    {
        var comparerCalls = 0;
        var hashCalls = 0;
        var comparer = new LambdaEqualityComparer<Value>((first, second) =>
        {
            comparerCalls++;
            return StringComparer.OrdinalIgnoreCase.Equals(first.Name, second.Name);
        }, value =>
        {
            hashCalls++;
            return value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(value.Name);
        });
        var first = new Value("alpha");
        var equal = new Value("ALPHA");
        var different = new Value("beta");
        Assert.True(comparer.Equals(first, equal));
        Assert.False(comparer.Equals(first, different));
        Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(equal));
        Assert.Equal(2, hashCalls);
        Assert.False(comparer.Equals(null, first));
        Assert.False(comparer.Equals(first, null));
        Assert.Equal(2, comparerCalls);
        Assert.True(comparer.Equals(null, null));
        Assert.Equal(2, comparerCalls);
        var set = new HashSet<Value>(comparer) { null!, null!, first, equal, different };
        Assert.Equal(3, set.Count);
        Assert.Contains(null!, set);
        Assert.Contains(first, set);
        Assert.Contains(different, set);
        Assert.True(hashCalls > 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-METADATA-VALIDATION", "validation-reason-first-query-is-order-independent")]
    public void ValidationReason_IsAvailableBeforeAnyValidityQuery(bool generic)
    {
        Assert.Null(MessageTypeCache<ReasonFirstMessage>.InvalidMessageTypeReason);
        Assert.True(MessageTypeCache<ReasonFirstMessage>.IsValidMessageType);
        string? first;
        string? later;
        if (generic)
        {
            first = MessageTypeCache<Tuple<ReasonFirstMessage>>.InvalidMessageTypeReason;
            Assert.False(MessageTypeCache<Tuple<ReasonFirstMessage>>.IsValidMessageType);
            later = MessageTypeCache<Tuple<ReasonFirstMessage>>.InvalidMessageTypeReason;
        }
        else
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("ViciOne.ServiceBus.ReasonFirstBoundary"), AssemblyBuilderAccess.RunAndCollect);
            Type type = assembly.DefineDynamicModule("Contracts")
                .DefineType("System.ServiceBusReview.RuntimeReasonFirstContract", TypeAttributes.Public | TypeAttributes.Class)
                .CreateType()!;
            first = MessageTypeCache.InvalidMessageTypeReason(type);
            Assert.False(MessageTypeCache.IsValidMessageType(type));
            later = MessageTypeCache.InvalidMessageTypeReason(type);
        }
        Assert.NotNull(first);
        Assert.Contains("System namespace", first, StringComparison.Ordinal);
        Assert.Equal(first, later);
    }

    private sealed record Value(string Name);
    private sealed class ReasonFirstMessage;
}
