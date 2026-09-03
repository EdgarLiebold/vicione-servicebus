using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.DurableSend;

public sealed class DurableSendContractsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-IDENTITY", "nonempty-id-and-lease-token")]
    public void IdAndLease_RequireNonemptyStableIdentities()
    {
        Guid idValue = Guid.Parse("11111111-2222-3333-4444-555555555555");
        DateTimeOffset expiresAt = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

        var id = new DurableSendId(idValue);
        var lease = new DurableSendLease(idValue, expiresAt);

        Assert.Equal(idValue, id.Value);
        Assert.Equal("11111111-2222-3333-4444-555555555555", id.ToString());
        Assert.Equal(idValue, lease.Token);
        Assert.Equal(expiresAt, lease.ExpiresAt);
        Assert.Equal("value", Assert.Throws<ArgumentException>(() => new DurableSendId(Guid.Empty)).ParamName);
        Assert.Equal("token", Assert.Throws<ArgumentException>(() => new DurableSendLease(Guid.Empty, expiresAt)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-CAPACITY", "positive-exact-store-limits")]
    public void StoreLimits_RequirePositiveExactCountAndLogicalByteBounds()
    {
        var minimum = new DurableSendStoreLimits(1, 1);

        Assert.Equal(1, minimum.MaximumStoredCount);
        Assert.Equal(1, minimum.MaximumStoredBytes);
        Assert.Equal("maximumStoredCount", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DurableSendStoreLimits(0, 1)).ParamName);
        Assert.Equal("maximumStoredBytes", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DurableSendStoreLimits(1, 0)).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DurableSendStoreLimits(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DurableSendStoreLimits(1, -1));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-OPERATIONS", "absolute-claim-and-page-bounds")]
    public void OperationLimits_AcceptOnlyThePublishedFiniteRanges()
    {
        Assert.Equal(1, DurableSendOperationLimits.ValidateClaimCount(1, "claim"));
        Assert.Equal(DurableSendOperationLimits.AbsoluteMaximumClaimCount,
            DurableSendOperationLimits.ValidateClaimCount(DurableSendOperationLimits.AbsoluteMaximumClaimCount, "claim"));
        Assert.Equal(1, DurableSendOperationLimits.ValidateQuarantinePageSize(1, "page"));
        Assert.Equal(DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize,
            DurableSendOperationLimits.ValidateQuarantinePageSize(
                DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize,
                "page"));

        Assert.Equal("claim", Assert.Throws<ArgumentOutOfRangeException>(() =>
            DurableSendOperationLimits.ValidateClaimCount(0, "claim")).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => DurableSendOperationLimits.ValidateClaimCount(
            DurableSendOperationLimits.AbsoluteMaximumClaimCount + 1,
            "claim"));
        Assert.Equal("page", Assert.Throws<ArgumentOutOfRangeException>(() =>
            DurableSendOperationLimits.ValidateQuarantinePageSize(0, "page")).ParamName);
        Assert.Throws<ArgumentOutOfRangeException>(() => DurableSendOperationLimits.ValidateQuarantinePageSize(
            DurableSendOperationLimits.AbsoluteMaximumQuarantinePageSize + 1,
            "page"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-SERIALIZATION", "exact-content-address-and-zero-body-boundaries")]
    public void SerializedIntent_AcceptsExactTextBoundsAndAZeroByteBody()
    {
        Uri address = AddressWithAbsoluteLength(SerializedDurableSend.MaximumDestinationAddressCharacters);
        var message = Message() with
        {
            DestinationAddress = address,
            ContentType = new string('a', SerializedDurableSend.MaximumContentTypeCharacters),
            Body = ReadOnlyMemory<byte>.Empty,
            Metadata = ReadOnlyMemory<byte>.Empty,
        };

        Assert.Same(message, message.Validate());
        Assert.Equal(SerializedDurableSend.MaximumDestinationAddressCharacters, address.AbsoluteUri.Length);
        Assert.Equal(0, message.StorageSize);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-SERIALIZATION", "invalid-identity-address-and-content-rejected")]
    public void SerializedIntent_RejectsEveryInvalidPersistedFieldBeforeStorage()
    {
        SerializedDurableSend valid = Message();

        Assert.Equal("Id", Assert.Throws<ArgumentException>(() => (valid with { Id = default }).Validate()).ParamName);
        Assert.Equal("ContractIdentity", Assert.Throws<ArgumentException>(() =>
            (valid with { ContractIdentity = default }).Validate()).ParamName);
        Assert.Equal("DestinationAddress", Assert.Throws<ArgumentException>(() =>
            (valid with { DestinationAddress = new Uri("relative", UriKind.Relative) }).Validate()).ParamName);
        Assert.Equal("DestinationAddress", Assert.Throws<ArgumentException>(() =>
            (valid with { DestinationAddress = null! }).Validate()).ParamName);
        Assert.Equal("DestinationAddress", Assert.Throws<ArgumentOutOfRangeException>(() =>
            (valid with
            {
                DestinationAddress = AddressWithAbsoluteLength(
                SerializedDurableSend.MaximumDestinationAddressCharacters + 1)
            }).Validate()).ParamName);
        Assert.Equal("ContentType", Assert.Throws<ArgumentException>(() =>
            (valid with { ContentType = " " }).Validate()).ParamName);
        Assert.Equal("ContentType", Assert.Throws<ArgumentException>(() =>
            (valid with { ContentType = "application/json\r\nInjected: true" }).Validate()).ParamName);
        Assert.Equal("ContentType", Assert.Throws<ArgumentOutOfRangeException>(() =>
            (valid with { ContentType = new string('a', SerializedDurableSend.MaximumContentTypeCharacters + 1) })
            .Validate()).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-CAPACITY", "body-plus-servicebus-metadata-ledger")]
    public void StorageSize_CountsOnlyBodyAndServiceBusMetadataBytes()
    {
        SerializedDurableSend message = Message() with
        {
            Body = new byte[13],
            Metadata = new byte[7],
            ContentType = new string('x', 200),
            DestinationAddress = new Uri("https://example.test/a/long/transport/address"),
        };

        Assert.Equal(20, message.StorageSize);
        Assert.Same(message, message.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-ADMISSION", "new-versus-idempotent-dispositions")]
    public void AdmissionResult_IdentifiesOnlyANewlyCommittedIntentAsNew()
    {
        DurableSendId id = Message().Id;

        Assert.True(new DurableSendAdmissionResult(id, DurableSendAdmissionDisposition.Accepted, 1, 2).IsNew);
        Assert.False(new DurableSendAdmissionResult(id, DurableSendAdmissionDisposition.AlreadyAccepted, 1, 2).IsNew);
        Assert.False(new DurableSendAdmissionResult(id, DurableSendAdmissionDisposition.AlreadyQuarantined, 1, 2).IsNew);
        Assert.Equal(DurableSendCompletionMode.TransportAcceptance,
            DurableSendDispatchResult.TransportAccepted.CompletionMode);
        Assert.Equal(DurableSendCompletionMode.ConsumerCompletion,
            DurableSendDispatchResult.AwaitConsumerCompletion.CompletionMode);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-SEND-QUARANTINE", "operator-evidence-is-payload-free")]
    public void QuarantineEvidence_ExposesNoPayloadOrMetadataProperty()
    {
        string[] propertyNames = typeof(DurableSendQuarantineEntry).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("Body", propertyNames);
        Assert.DoesNotContain("Metadata", propertyNames);
        Assert.Contains(nameof(DurableSendQuarantineEntry.FailureKind), propertyNames);
        Assert.Contains(nameof(DurableSendQuarantineEntry.ContractIdentity), propertyNames);
    }

    private static SerializedDurableSend Message() => new()
    {
        Id = new DurableSendId(Guid.Parse("77777777-2222-3333-4444-555555555555")),
        ContractIdentity = new MessageContractIdentity("vicione.tests.durable", 1),
        DestinationAddress = new Uri("https://example.test/durable"),
        ContentType = "application/octet-stream",
        Body = new byte[] { 1 },
    };

    private static Uri AddressWithAbsoluteLength(int length)
    {
        const string prefix = "https://example.test/";
        return new Uri(prefix + new string('a', length - prefix.Length));
    }
}
