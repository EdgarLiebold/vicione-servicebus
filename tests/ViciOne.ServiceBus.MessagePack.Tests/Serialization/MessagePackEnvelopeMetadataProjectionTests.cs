using Microsoft.Extensions.Time.Testing;
using MessagePack;
using System.Text.Json;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackEnvelopeMetadataProjectionTests
{
    private static readonly DateTimeOffset ProjectionTime =
        new(2026, 8, 24, 9, 10, 11, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "messagepack-matches-system-text-json")]
    public void SendContext_ProjectsTheSameMetadataForBothWireFormats()
    {
        var timeProvider = new FakeTimeProvider(ProjectionTime);
        var context = new MessageSendContext<TestMessage>(new TestMessage("payload"))
        {
            MessageId = Guid.Parse("d45e2fd1-37c8-4ee8-8544-0fa39afda4bb"),
            RequestId = Guid.Parse("7f28986b-e1f4-47bd-bce1-ef1936031f04"),
            CorrelationId = Guid.Parse("8e3b4d14-9325-4417-a12f-d65bb5a94d61"),
            ConversationId = Guid.Parse("cead71d9-bdbb-4b67-8206-72f7afea37a8"),
            InitiatorId = Guid.Parse("35a91ad7-b497-43a3-b93d-039405760ad2"),
            SourceAddress = new Uri("loopback://source"),
            DestinationAddress = new Uri("loopback://destination"),
            ResponseAddress = new Uri("loopback://response"),
            FaultAddress = new Uri("loopback://fault"),
            TimeToLive = TimeSpan.FromMinutes(7),
        };
        context.Headers.Set("ViciOne-Test", "metadata");
        context.SetTimeProvider(timeProvider);

        var json = new JsonMessageEnvelope(context, context.Message);
        var messagePack = new MessagePackEnvelope(context, context.Message);

        Assert.Equal(json.MessageId, messagePack.MessageId);
        Assert.Equal(json.RequestId, messagePack.RequestId);
        Assert.Equal(json.CorrelationId, messagePack.CorrelationId);
        Assert.Equal(json.ConversationId, messagePack.ConversationId);
        Assert.Equal(json.InitiatorId, messagePack.InitiatorId);
        Assert.Equal(json.SourceAddress, messagePack.SourceAddress);
        Assert.Equal(json.DestinationAddress, messagePack.DestinationAddress);
        Assert.Equal(json.ResponseAddress, messagePack.ResponseAddress);
        Assert.Equal(json.FaultAddress, messagePack.FaultAddress);
        Assert.Equal(json.MessageTypes, messagePack.MessageTypes);
        Assert.Equal(json.ExpirationTime, messagePack.ExpirationTime);
        Assert.Equal(json.SentTime, messagePack.SentTime);
        Assert.Equal(json.Headers["ViciOne-Test"], messagePack.Headers!["ViciOne-Test"]);
        AssertHost(json.Host, messagePack.Host);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "messagepack-one-utc-read")]
    public void MissingSentTime_CapturesExactlyOneUtcInstant()
    {
        var timeProvider = new FakeTimeProvider(ProjectionTime)
        {
            AutoAdvanceAmount = TimeSpan.FromSeconds(1),
        };
        var context = new TestMessageContext(timeProvider);

        var envelope = new MessagePackEnvelope(context, new TestMessage("payload"), ["urn:message:test"]);

        Assert.Equal(ProjectionTime, envelope.SentTime);
        Assert.Equal(ProjectionTime + TimeSpan.FromSeconds(1), timeProvider.GetUtcNow());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "messagepack-expired-overlay-has-no-grace")]
    public void Overlay_UsesTheExactExpiredInstantWithoutSerializerFallback()
    {
        var timeProvider = new FakeTimeProvider(ProjectionTime);
        var context = new MessageSendContext<TestMessage>(new TestMessage("payload"))
        {
            TimeToLive = TimeSpan.FromSeconds(-30),
        };
        context.SetTimeProvider(timeProvider);
        var envelope = new MessagePackEnvelope(context, context.Message)
        {
            ExpirationTime = ProjectionTime + TimeSpan.FromHours(1),
        };

        envelope.Update(context);

        Assert.Equal(ProjectionTime - TimeSpan.FromSeconds(30), envelope.ExpirationTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-ENVELOPE-METADATA-PROJECTION", "messagepack-host-cache-roundtrip-preserves-wire-mutability")]
    public void HostSnapshots_RoundTripWithoutSerializingTheirReadOnlyMode(int sourceKind)
    {
        HostInfo source = sourceKind switch
        {
            0 => HostMetadataCache.Host,
            1 => HostMetadataCache.Empty,
            2 => new BusHostInfo
            {
                MachineName = "wire-machine", ProcessName = "wire-process", ProcessId = 79,
                Assembly = "wire-assembly", AssemblyVersion = "2.3.4",
                FrameworkVersion = "wire-framework", ViciOneServiceBusVersion = "wire-servicebus",
                OperatingSystemVersion = "wire-os"
            },
            _ => throw new ArgumentOutOfRangeException(nameof(sourceKind))
        };
        var expectedWire = new BusHostInfo
        {
            MachineName = source.MachineName, ProcessName = source.ProcessName, ProcessId = source.ProcessId,
            Assembly = source.Assembly, AssemblyVersion = source.AssemblyVersion,
            FrameworkVersion = source.FrameworkVersion, ViciOneServiceBusVersion = source.ViciOneServiceBusVersion,
            OperatingSystemVersion = source.OperatingSystemVersion
        };
        Assert.IsType<BusHostInfo>(source);

        byte[] actualBytes = MessagePackSerializationRuntime.Serialize<HostInfo>(source);
        byte[] expectedBytes = MessagePackSerializationRuntime.Serialize<HostInfo>(expectedWire);

        string actualJson = MessagePackSerializer.ConvertToJson(actualBytes, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(MessagePackSerializer.ConvertToJson(expectedBytes, cancellationToken: TestContext.Current.CancellationToken), actualJson);
        using var wireJson = JsonDocument.Parse(actualJson);
        Assert.All(wireJson.RootElement.EnumerateObject(), property =>
        {
            Assert.DoesNotContain("readonly", property.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("frozen", property.Name, StringComparison.OrdinalIgnoreCase);
        });
        HostInfo restored = MessagePackSerializationRuntime.Deserialize<HostInfo>(actualBytes);
        var mutable = Assert.IsType<BusHostInfo>(restored);
        AssertHost(expectedWire, restored);
        mutable.MachineName = "changed-after-deserialization";
        mutable.ProcessId = int.MinValue;
        Assert.Equal("changed-after-deserialization", mutable.MachineName);
        Assert.Equal(int.MinValue, mutable.ProcessId);
        AssertHost(expectedWire, source);
        if (sourceKind < 2)
            Assert.Same(source, sourceKind == 0 ? HostMetadataCache.Host : HostMetadataCache.Empty);
    }

    private sealed record TestMessage(string Value);

    private sealed class TestMessageContext : BasePipeContext, MessageContext
    {
        private readonly DictionarySendHeaders _headers = new();

        public TestMessageContext(TimeProvider timeProvider)
        {
            this.SetTimeProvider(timeProvider);
        }

        public Guid? MessageId => null;
        public Guid? RequestId => null;
        public Guid? CorrelationId => null;
        public Guid? ConversationId => null;
        public Guid? InitiatorId => null;
        public DateTimeOffset? ExpirationTime => null;
        public Uri? SourceAddress => null;
        public Uri? DestinationAddress => null;
        public Uri? ResponseAddress => null;
        public Uri? FaultAddress => null;
        public DateTimeOffset? SentTime => null;
        public Headers Headers => _headers;
        public HostInfo Host => HostMetadataCache.Host;
    }

    private static void AssertHost(HostInfo? expected, HostInfo? actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.MachineName, actual.MachineName);
        Assert.Equal(expected.ProcessName, actual.ProcessName);
        Assert.Equal(expected.ProcessId, actual.ProcessId);
        Assert.Equal(expected.Assembly, actual.Assembly);
        Assert.Equal(expected.AssemblyVersion, actual.AssemblyVersion);
        Assert.Equal(expected.FrameworkVersion, actual.FrameworkVersion);
        Assert.Equal(expected.ViciOneServiceBusVersion, actual.ViciOneServiceBusVersion);
        Assert.Equal(expected.OperatingSystemVersion, actual.OperatingSystemVersion);
    }
}
