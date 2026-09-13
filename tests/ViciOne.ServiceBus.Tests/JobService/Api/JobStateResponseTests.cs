using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Api;

public sealed class JobStateResponseTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-STATE-API", "typed-projection-preserves-every-lifecycle-field-and-checkpoint")]
    public void TypedProjection_PreservesEveryLifecycleFieldAndTypedCheckpoint()
    {
        Guid jobId = NewId.NextGuid();
        DateTimeOffset submitted = new(2034, 1, 2, 3, 4, 5, TimeSpan.Zero);
        DateTimeOffset started = submitted.AddMinutes(1);
        DateTimeOffset completed = submitted.AddMinutes(3);
        DateTimeOffset faulted = submitted.AddMinutes(2);
        DateTimeOffset nextStart = submitted.AddDays(1);
        DateTimeOffset scheduleStart = submitted.AddDays(-1);
        DateTimeOffset scheduleEnd = submitted.AddDays(30);
        var serializedCheckpoint = new Dictionary<string, object> { ["offset"] = 41L };
        var checkpoint = new Checkpoint(41);
        var source = new JobStateResponse
        {
            JobId = jobId,
            Submitted = submitted,
            Started = started,
            Completed = completed,
            Duration = TimeSpan.FromMinutes(2),
            Faulted = faulted,
            Reason = "transient",
            LastRetryAttempt = 3,
            Status = JobLifecycleStatus.Completed,
            ProgressValue = 7,
            ProgressLimit = 10,
            Checkpoint = serializedCheckpoint,
            NextStartDate = nextStart,
            IsRecurring = true,
            StartDate = scheduleStart,
            EndDate = scheduleEnd,
        };

        JobState<Checkpoint> typed = new JobStateResponse<Checkpoint>(source, checkpoint);
        JobState untyped = typed;

        Assert.Equal(jobId, typed.JobId);
        Assert.Equal(submitted, typed.Submitted);
        Assert.Equal(started, typed.Started);
        Assert.Equal(completed, typed.Completed);
        Assert.Equal(TimeSpan.FromMinutes(2), typed.Duration);
        Assert.Equal(faulted, typed.Faulted);
        Assert.Equal("transient", typed.Reason);
        Assert.Equal(3, typed.LastRetryAttempt);
        Assert.Equal(JobLifecycleStatus.Completed, typed.Status);
        Assert.Equal(7, typed.ProgressValue);
        Assert.Equal(10, typed.ProgressLimit);
        Assert.Same(serializedCheckpoint, untyped.Checkpoint);
        Assert.Same(checkpoint, typed.Checkpoint);
        Assert.Equal(nextStart, typed.NextStartDate);
        Assert.True(typed.IsRecurring);
        Assert.Equal(scheduleStart, typed.StartDate);
        Assert.Equal(scheduleEnd, typed.EndDate);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-STATE-API", "typed-projection-supports-an-absent-checkpoint")]
    public void TypedProjection_ExposesAnAbsentTypedCheckpoint()
    {
        JobState<Checkpoint> typed = new JobStateResponse<Checkpoint>(new JobStateResponse());

        Assert.Null(typed.Checkpoint);
        Assert.Null(((JobState)typed).Checkpoint);
    }

    private sealed record Checkpoint(long Offset);
}
