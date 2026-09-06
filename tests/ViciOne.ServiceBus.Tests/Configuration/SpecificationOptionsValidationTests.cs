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

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-OPTIONS", "retry-configuration-requires-callback-and-policy")]
    public void JobOptions_RetryConfigurationRejectsMissingInputs()
    {
        var options = new JobOptions<TestJob>();

        Assert.Throws<ArgumentNullException>(() => options.ConfigureRetry(null!));
        Assert.Throws<ConfigurationException>(() => options.ConfigureRetry(_ => { }));
        Assert.Same(Retry.None, options.RetryPolicy);
    }

    [Theory]
    [InlineData(InvalidJobServiceOption.SlotWait, "SlotWaitTime")]
    [InlineData(InvalidJobServiceOption.StatusCheck, "StatusCheckInterval")]
    [InlineData(InvalidJobServiceOption.HeartbeatInterval, "HeartbeatInterval")]
    [InlineData(InvalidJobServiceOption.HeartbeatTimeout, "HeartbeatTimeout")]
    [InlineData(InvalidJobServiceOption.RejectedJobDelay, "RejectedJobDelay")]
    [InlineData(InvalidJobServiceOption.TimeProvider, "TimeProvider")]
    [InlineData(InvalidJobServiceOption.Concurrency, "ConcurrentMessageLimit")]
    [InlineData(InvalidJobServiceOption.RetryCount, "SuspectJobRetryCount")]
    [InlineData(InvalidJobServiceOption.RetryDelay, "SuspectJobRetryDelay")]
    [InlineData(InvalidJobServiceOption.TypeEndpoint, "JobTypeEndpointName")]
    [InlineData(InvalidJobServiceOption.StateEndpoint, "JobEndpointName")]
    [InlineData(InvalidJobServiceOption.AttemptEndpoint, "JobAttemptEndpointName")]
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
            case InvalidJobServiceOption.RejectedJobDelay: options.RejectedJobDelay = TimeSpan.Zero; break;
            case InvalidJobServiceOption.TimeProvider: options.TimeProvider = null!; break;
            case InvalidJobServiceOption.Concurrency: options.ConcurrentMessageLimit = 0; break;
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
    public void JobServiceOptions_CoherentMaterializedValuesPass()
    {
        JobServiceOptions options = ValidJobServiceOptions();

        Assert.Empty(((ISpecification)options).Validate());
        Assert.True(options.FinalizeCompleted);
        Assert.Equal(16, options.ConcurrentMessageLimit);
        Assert.Equal(TimeSpan.FromMinutes(1), options.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromSeconds(3), options.RejectedJobDelay);
        Assert.Same(TimeProvider.System, options.TimeProvider);
    }

    [Theory]
    [InlineData(InvalidJobSagaOption.SlotWait, "SlotWaitTime")]
    [InlineData(InvalidJobSagaOption.StatusCheck, "StatusCheckInterval")]
    [InlineData(InvalidJobSagaOption.HeartbeatTimeout, "HeartbeatTimeout")]
    [InlineData(InvalidJobSagaOption.Concurrency, "ConcurrentMessageLimit")]
    [InlineData(InvalidJobSagaOption.RetryCount, "SuspectJobRetryCount")]
    [InlineData(InvalidJobSagaOption.RetryDelay, "SuspectJobRetryDelay")]
    [RequirementCoverage("REQ-VSB-JOB-SAGA-OPTIONS", "each-invariant-rejected-before-endpoint-build")]
    public void JobSagaOptions_RejectEveryInvalidInvariant(InvalidJobSagaOption invalid, string property)
    {
        var options = new JobSagaOptions();
        switch (invalid)
        {
            case InvalidJobSagaOption.SlotWait: options.SlotWaitTime = TimeSpan.Zero; break;
            case InvalidJobSagaOption.StatusCheck: options.StatusCheckInterval = TimeSpan.FromSeconds(29); break;
            case InvalidJobSagaOption.HeartbeatTimeout: options.HeartbeatTimeout = TimeSpan.Zero; break;
            case InvalidJobSagaOption.Concurrency: options.ConcurrentMessageLimit = 0; break;
            case InvalidJobSagaOption.RetryCount: options.SuspectJobRetryCount = -1; break;
            case InvalidJobSagaOption.RetryDelay: options.SuspectJobRetryDelay = TimeSpan.Zero; break;
        }

        ValidationResult[] failures = ((ISpecification)options).Validate().ToArray();

        Assert.Contains(failures, failure => Identifies(failure, property));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SAGA-OPTIONS", "defaults-are-coherent")]
    public void JobSagaOptions_DefaultsAreCoherent()
    {
        var options = new JobSagaOptions();

        Assert.Empty(((ISpecification)options).Validate());
        Assert.True(options.FinalizeCompleted);
        Assert.Equal(16, options.ConcurrentMessageLimit);
        Assert.Equal(TimeSpan.FromMinutes(5), options.HeartbeatTimeout);
    }

    static JobServiceOptions ValidJobServiceOptions() => new()
    {
        JobTypeEndpointName = "job-type",
        JobEndpointName = "job-state",
        JobAttemptEndpointName = "job-attempt",
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
        RejectedJobDelay,
        TimeProvider,
        Concurrency,
        RetryCount,
        RetryDelay,
        TypeEndpoint,
        StateEndpoint,
        AttemptEndpoint,
    }

    public enum InvalidJobSagaOption { SlotWait, StatusCheck, HeartbeatTimeout, Concurrency, RetryCount, RetryDelay }
}
