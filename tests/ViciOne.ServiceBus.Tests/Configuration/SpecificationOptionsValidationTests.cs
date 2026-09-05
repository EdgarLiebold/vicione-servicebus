using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SpecificationOptionsValidationTests
{
    [Theory]
    [InlineData(InvalidBatchOption.MessageLimit, "MessageLimit")]
    [InlineData(InvalidBatchOption.ConcurrencyLimit, "ConcurrencyLimit")]
    [InlineData(InvalidBatchOption.TimeLimit, "TimeLimit")]
    [InlineData(InvalidBatchOption.TimeLimitStart, "TimeLimitStart")]
    [RequirementCoverage("REQ-VSB-BATCH-OPTIONS", "each-invariant-rejected-before-pipe-build")]
    public void BatchOptions_RejectEveryInvalidInvariant(InvalidBatchOption invalid, string property)
    {
        var options = new BatchOptions();
        switch (invalid)
        {
            case InvalidBatchOption.MessageLimit: options.MessageLimit = 0; break;
            case InvalidBatchOption.ConcurrencyLimit: options.ConcurrencyLimit = 0; break;
            case InvalidBatchOption.TimeLimit: options.TimeLimit = TimeSpan.Zero; break;
            case InvalidBatchOption.TimeLimitStart: options.TimeLimitStart = (BatchTimeLimitStart)42; break;
        }

        ValidationResult[] failures = options.Validate().ToArray();

        Assert.Contains(failures, failure => Identifies(failure, property));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-OPTIONS", "defaults-are-coherent")]
    public void BatchOptions_DefaultsAreCoherent() =>
        Assert.Empty(new BatchOptions().Validate());

    [Theory]
    [InlineData(InvalidJobOption.Concurrency, "ConcurrentJobLimit")]
    [InlineData(InvalidJobOption.Timeout, "JobTimeout")]
    [InlineData(InvalidJobOption.CancellationTimeout, "JobCancellationTimeout")]
    [InlineData(InvalidJobOption.GlobalConcurrency, "GlobalConcurrentJobLimit")]
    [InlineData(InvalidJobOption.Name, "JobTypeName")]
    [InlineData(InvalidJobOption.ProgressCount, "ProgressBuffer.UpdateLimit")]
    [InlineData(InvalidJobOption.ProgressTime, "ProgressBuffer.TimeLimit")]
    [RequirementCoverage("REQ-VSB-JOB-OPTIONS", "each-invariant-rejected-before-endpoint-build")]
    public void JobOptions_RejectEveryInvalidInvariant(InvalidJobOption invalid, string property)
    {
        var options = new JobOptions<TestJob>();
        switch (invalid)
        {
            case InvalidJobOption.Concurrency: options.ConcurrentJobLimit = 0; break;
            case InvalidJobOption.Timeout: options.JobTimeout = TimeSpan.Zero; break;
            case InvalidJobOption.CancellationTimeout: options.JobCancellationTimeout = TimeSpan.Zero; break;
            case InvalidJobOption.GlobalConcurrency: options.GlobalConcurrentJobLimit = 0; break;
            case InvalidJobOption.Name: options.JobTypeName = " "; break;
            case InvalidJobOption.ProgressCount: options.ProgressBuffer.UpdateLimit = 0; break;
            case InvalidJobOption.ProgressTime: options.ProgressBuffer.TimeLimit = TimeSpan.Zero; break;
        }

        ValidationResult[] failures = ((ISpecification)options).Validate().ToArray();

        Assert.Contains(failures, failure => Identifies(failure, property));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-OPTIONS", "defaults-are-coherent")]
    public void JobOptions_DefaultsAreCoherent() =>
        Assert.Empty(((ISpecification)new JobOptions<TestJob>()).Validate());

    [Theory]
    [InlineData(InvalidJobServiceOption.SlotWait, "SlotWaitTime")]
    [InlineData(InvalidJobServiceOption.StatusCheck, "StatusCheckInterval")]
    [InlineData(InvalidJobServiceOption.HeartbeatInterval, "HeartbeatInterval")]
    [InlineData(InvalidJobServiceOption.HeartbeatTimeout, "HeartbeatTimeout")]
    [InlineData(InvalidJobServiceOption.Partitions, "SagaPartitionCount")]
    [InlineData(InvalidJobServiceOption.RetryCount, "SuspectJobRetryCount")]
    [InlineData(InvalidJobServiceOption.RetryDelay, "SuspectJobRetryDelay")]
    [InlineData(InvalidJobServiceOption.TypeEndpoint, "JobTypeSagaEndpointName")]
    [InlineData(InvalidJobServiceOption.StateEndpoint, "JobStateSagaEndpointName")]
    [InlineData(InvalidJobServiceOption.AttemptEndpoint, "JobAttemptSagaEndpointName")]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-OPTIONS", "each-invariant-rejected-before-endpoint-build")]
    public void JobServiceOptions_RejectEveryInvalidInvariant(InvalidJobServiceOption invalid, string property)
    {
        JobServiceOptions options = invalid is InvalidJobServiceOption.TypeEndpoint
            or InvalidJobServiceOption.StateEndpoint
            or InvalidJobServiceOption.AttemptEndpoint
                ? new JobServiceOptions()
                : ValidJobServiceOptions();
        switch (invalid)
        {
            case InvalidJobServiceOption.SlotWait: options.SlotWaitTime = TimeSpan.Zero; break;
            case InvalidJobServiceOption.StatusCheck: options.StatusCheckInterval = TimeSpan.FromSeconds(29); break;
            case InvalidJobServiceOption.HeartbeatInterval: options.HeartbeatInterval = TimeSpan.Zero; break;
            case InvalidJobServiceOption.HeartbeatTimeout: options.HeartbeatTimeout = TimeSpan.Zero; break;
            case InvalidJobServiceOption.Partitions: options.SagaPartitionCount = 0; break;
            case InvalidJobServiceOption.RetryCount: options.SuspectJobRetryCount = -1; break;
            case InvalidJobServiceOption.RetryDelay: options.SuspectJobRetryDelay = TimeSpan.Zero; break;
            case InvalidJobServiceOption.TypeEndpoint:
            case InvalidJobServiceOption.StateEndpoint:
            case InvalidJobServiceOption.AttemptEndpoint:
                break;
        }

        ValidationResult[] failures = ((ISpecification)options).Validate().ToArray();

        Assert.Contains(failures, failure => Identifies(failure, property));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-OPTIONS", "coherent-materialized-values-pass")]
    public void JobServiceOptions_CoherentMaterializedValuesPass() =>
        Assert.Empty(((ISpecification)ValidJobServiceOptions()).Validate());

    static JobServiceOptions ValidJobServiceOptions() => new()
    {
        JobTypeSagaEndpointName = "job-type",
        JobStateSagaEndpointName = "job-state",
        JobAttemptSagaEndpointName = "job-attempt",
    };

    static bool Identifies(ValidationResult failure, string property) =>
        failure.Key.Contains(property, StringComparison.Ordinal)
        || (failure.Value?.Contains(property, StringComparison.Ordinal) ?? false)
        || failure.Message.Contains(property, StringComparison.Ordinal);

    public sealed record TestJob;

    public enum InvalidBatchOption { MessageLimit, ConcurrencyLimit, TimeLimit, TimeLimitStart }

    public enum InvalidJobOption { Concurrency, Timeout, CancellationTimeout, GlobalConcurrency, Name, ProgressCount, ProgressTime }

    public enum InvalidJobServiceOption
    {
        SlotWait,
        StatusCheck,
        HeartbeatInterval,
        HeartbeatTimeout,
        Partitions,
        RetryCount,
        RetryDelay,
        TypeEndpoint,
        StateEndpoint,
        AttemptEndpoint,
    }
}
