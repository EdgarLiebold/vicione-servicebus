using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class MessagePackDomainContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-JOBS", "nested-interface-job-payload")]
    public async Task JobPayload_RoundTripsThroughDictionaryAndRestoresNestedInterfacesAsync()
    {
        var jobInitialization = await MessageInitializerCache<ConvertVideo>.InitializeAsync(
            new
            {
                Path = "input.mp4",
                GroupId = "group-1",
                Index = 0,
                Count = 1,
                Details = new[] { new { Value = "first" }, new { Value = "second" } },
            },
            TestContext.Current.CancellationToken);
        ConvertVideo job = jobInitialization.Message;
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
        var restoredJob = commandRoundTrip.Context.DeserializeObject<ConvertVideo>(commandRoundTrip.Message.Job);

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

    public interface VideoDetail
    {
        string Value { get; set; }
    }

    public interface ConvertVideo
    {
        string GroupId { get; }

        int Index { get; }

        int Count { get; }

        string Path { get; }

        IList<VideoDetail> Details { get; }
    }

    private sealed class NonSerializableException(string message) : Exception(message);
}
