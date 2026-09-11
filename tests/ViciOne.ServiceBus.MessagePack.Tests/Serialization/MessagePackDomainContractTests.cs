using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackDomainContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-COURIER", "nested-routing-slip-without-product-coupling")]
    public void RoutingSlip_RoundTripsNestedContractsWithoutMessagePackDependingOnCourier()
    {
        Guid trackingNumber = Guid.Parse("735b5180-9212-4ce2-8647-2101a0c8640a");
        var builder = new RoutingSlipBuilder(trackingNumber);
        builder.AddActivity(
            "convert-video",
            new Uri("loopback://courier/convert-video"),
            new { Path = "input.mp4" });
        builder.SetVariable("tenant", "north");
        builder.AddSubscription(
            new Uri("loopback://courier/events"),
            RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted);
        RoutingSlip source = builder.Build();

        RoutingSlip result = MessagePackRoundTrip.Execute(source);

        Assert.Equal(trackingNumber, result.TrackingNumber);
        Assert.Equal(source.CreateTimestamp, result.CreateTimestamp);
        Activity activity = Assert.Single(result.Itinerary);
        Assert.Equal("convert-video", activity.Name);
        Assert.Equal(new Uri("loopback://courier/convert-video"), activity.Address);
        Assert.Equal("input.mp4", activity.Arguments["path"]);
        Assert.Equal("north", result.Variables["tenant"]);
        Subscription subscription = Assert.Single(result.Subscriptions);
        Assert.Equal(new Uri("loopback://courier/events"), subscription.Address);
        Assert.Equal(RoutingSlipEvents.Completed | RoutingSlipEvents.Faulted, subscription.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-JOBS", "nested-interface-job-payload")]
    public async Task JobPayload_RoundTripsThroughDictionaryAndRestoresNestedInterfacesAsync()
    {
        var jobInitialization = await MessageInitializerCache<IConvertVideo>.InitializeAsync(
            new
            {
                Path = "input.mp4",
                GroupId = "group-1",
                Index = 0,
                Count = 1,
                Details = new[] { new { Value = "first" }, new { Value = "second" } },
            },
            TestContext.Current.CancellationToken);
        IConvertVideo job = jobInitialization.Message;
        var jobRoundTrip = MessagePackRoundTrip.ExecuteWithContext(job);
        var command = new StartJobCommand
        {
            JobId = NewId.NextGuid(),
            AttemptId = NewId.NextGuid(),
            Job = jobRoundTrip.Context.ToDictionary(jobRoundTrip.Message),
            JobTypeId = NewId.NextGuid(),
        };
        StartJob commandContract = command;

        var commandRoundTrip = MessagePackRoundTrip.ExecuteWithContext(commandContract);
        var restoredJob = commandRoundTrip.Context.DeserializeObject<IConvertVideo>(commandRoundTrip.Message.Job);

        Assert.NotNull(restoredJob);
        Assert.Equal("input.mp4", restoredJob.Path);
        Assert.Equal("group-1", restoredJob.GroupId);
        Assert.Equal(0, restoredJob.Index);
        Assert.Equal(1, restoredJob.Count);
        Assert.Equal(["first", "second"], restoredJob.Details.Select(detail => detail.Value));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FAULTS", "serializable-and-nonserializable-exceptions")]
    public void ReceiveFault_RoundTripsExceptionInformation(bool useSerializableException)
    {
        Exception exception = useSerializableException
            ? new InvalidOperationException("serializable failure")
            : new NonSerializableException("nonserializable failure");
        var source = new ReceiveFaultEvent(
            HostMetadataCache.Host,
            new AggregateException(exception),
            "application/test",
            Guid.NewGuid(),
            ["urn:message:Faulted"]);
        ReceiveFault contract = source;

        var result = MessagePackRoundTrip.Execute(contract);

        Assert.Equal(source.FaultId, result.FaultId);
        Assert.Equal("application/test", result.ContentType);
        Assert.Equal(source.FaultedMessageId, result.FaultedMessageId);
        Assert.Equal(["urn:message:Faulted"], result.FaultMessageTypes);
        var restored = Assert.Single(result.Exceptions);
        Assert.Contains("failure", restored.Message, StringComparison.Ordinal);
        Assert.Contains(exception.GetType().FullName!, restored.ExceptionType, StringComparison.Ordinal);
    }

    public interface IVideoDetail
    {
        string Value { get; set; }
    }

    public interface IConvertVideo
    {
        string GroupId { get; }

        int Index { get; }

        int Count { get; }

        string Path { get; }

        IList<IVideoDetail> Details { get; }
    }

    private sealed class NonSerializableException(string message) : Exception(message);
}
