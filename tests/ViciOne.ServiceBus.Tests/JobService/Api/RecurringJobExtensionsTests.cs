using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Api;

public sealed class RecurringJobExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-API", "request-cron-preserves-deterministic-id-job-and-token")]
    public async Task AddOrUpdateRecurringJobAsync_RequestCronPreservesTheCompleteCommandAsync()
    {
        var client = new RecordingSubmitJobClient<ApiJob>();
        var job = new ApiJob { Label = "recurring" };
        using var cancellation = new CancellationTokenSource();

        Guid result = await client.AddOrUpdateRecurringJobAsync(
            "nightly-index",
            job,
            "0 15 2 ? * *",
            cancellation.Token);

        Assert.Equal(RecurringJobIdentity<ApiJob>.CreateId("nightly-index"), result);
        SubmitJob<ApiJob> command = Assert.IsAssignableFrom<SubmitJob<ApiJob>>(client.Request);
        Assert.Equal(result, command.JobId);
        Assert.Same(job, command.Job);
        Assert.Equal("0 15 2 ? * *", command.Schedule?.CronExpression);
        Assert.Equal(cancellation.Token, client.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-API", "publish-configurator-preserves-schedule-properties-and-token")]
    public async Task AddOrUpdateRecurringJobAsync_PublishConfiguratorPreservesScheduleAndPropertiesAsync()
    {
        var endpoint = new RecordingPublishEndpoint();
        using var cancellation = new CancellationTokenSource();

        Guid result = await endpoint.AddOrUpdateRecurringJobAsync(
            "weekday-report",
            new ApiJob { Label = "report" },
            schedule => schedule.OnDaysAt(6, 30, days: [DayOfWeek.Monday, DayOfWeek.Friday])
                .InTimeZone(TimeZoneInfo.Utc),
            properties => properties.Set("tenant", "north"),
            cancellation.Token);

        SubmitJob<ApiJob> command = Assert.IsAssignableFrom<SubmitJob<ApiJob>>(endpoint.Message);
        Assert.Equal(result, command.JobId);
        Assert.Equal("0 30 6 ? * 2,6", command.Schedule?.CronExpression);
        Assert.Equal(TimeZoneInfo.Utc.Id, command.Schedule?.TimeZoneId);
        IReadOnlyDictionary<string, object> properties =
            Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(command.JobProperties);
        Assert.Equal("north", properties["tenant"]);
        Assert.Equal(cancellation.Token, endpoint.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULED-JOB-API", "typed-publish-preserves-explicit-id-utc-start-job-and-token")]
    public async Task ScheduleJobAsync_TypedPublishPreservesTheCompleteCommandAsync()
    {
        var endpoint = new RecordingPublishEndpoint();
        Guid jobId = NewId.NextGuid();
        var start = new DateTimeOffset(2045, 4, 5, 6, 7, 8, TimeSpan.FromHours(2));
        var job = new ApiJob { Label = "scheduled" };
        using var cancellation = new CancellationTokenSource();

        Guid result = await endpoint.ScheduleJobAsync(jobId, start, job, cancellation.Token);

        Assert.Equal(jobId, result);
        SubmitJob<ApiJob> command = Assert.IsAssignableFrom<SubmitJob<ApiJob>>(endpoint.Message);
        Assert.Same(job, command.Job);
        Assert.Equal(start.UtcDateTime, command.Schedule?.Start?.UtcDateTime);
        Assert.Equal(TimeSpan.Zero, command.Schedule?.Start?.Offset);
        Assert.Equal(cancellation.Token, endpoint.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULED-JOB-API", "values-request-initializes-contract-and-preserves-explicit-id")]
    public async Task ScheduleJobFromValuesAsync_RequestInitializesTheTypedContractAsync()
    {
        var client = new RecordingSubmitJobClient<ApiJob>();
        Guid jobId = NewId.NextGuid();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddHours(1);

        Guid result = await client.ScheduleJobFromValuesAsync<ApiJob>(
            jobId,
            start,
            new { Label = "initialized-schedule" },
            TestContext.Current.CancellationToken);

        Assert.Equal(jobId, result);
        SubmitJob<ApiJob> command = Assert.IsAssignableFrom<SubmitJob<ApiJob>>(client.Request);
        Assert.Equal("initialized-schedule", command.Job.Label);
        Assert.Equal(start, command.Schedule?.Start);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-API", "schedule-callback-must-define-recurrence")]
    public async Task AddOrUpdateRecurringJobAsync_RejectsAConfigurationWithoutRecurrenceAsync()
    {
        var endpoint = new RecordingPublishEndpoint();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() =>
            endpoint.AddOrUpdateRecurringJobAsync(
                "not-recurring",
                new ApiJob(),
                schedule => schedule.Start = DateTimeOffset.UtcNow.AddHours(1),
                TestContext.Current.CancellationToken));

        Assert.Contains("cron expression", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(endpoint.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-API", "null-endpoint-job-configurator-and-cron-are-rejected")]
    public async Task AddOrUpdateRecurringJobAsync_RejectsMissingRequiredArgumentsAsync()
    {
        var endpoint = new RecordingPublishEndpoint();
        var job = new ApiJob();

        Assert.Equal("publishEndpoint", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            RecurringJobExtensions.AddOrUpdateRecurringJobAsync(
                (IPublishEndpoint)null!,
                "job",
                job,
                "0 0 0 ? * *",
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("job", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.AddOrUpdateRecurringJobAsync<ApiJob>(
                "job",
                null!,
                "0 0 0 ? * *",
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("cronExpression", (await Assert.ThrowsAsync<ArgumentException>(() =>
            endpoint.AddOrUpdateRecurringJobAsync(
                "job",
                job,
                " ",
                TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("configure", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.AddOrUpdateRecurringJobAsync(
                jobName: "job",
                job: job,
                configure: null!,
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-API", "missing-stable-name-is-rejected")]
    public async Task RecurringJobApis_RejectMissingNamesAsync(string? jobName)
    {
        var endpoint = new RecordingPublishEndpoint();

        ArgumentException exception = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            endpoint.AddOrUpdateRecurringJobAsync(
                jobName!,
                new ApiJob(),
                "0 0 0 ? * *",
                TestContext.Current.CancellationToken));

        Assert.Equal("jobName", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULED-JOB-API", "empty-ids-null-jobs-and-null-values-are-rejected")]
    public async Task ScheduledJobApis_RejectInvalidRequiredArgumentsAsync()
    {
        var endpoint = new RecordingPublishEndpoint();
        var client = new RecordingSubmitJobClient<ApiJob>();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddHours(1);

        Assert.Equal("jobId", (await Assert.ThrowsAsync<ArgumentException>(() =>
            endpoint.ScheduleJobAsync(Guid.Empty, start, new ApiJob(), TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("job", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.ScheduleJobAsync<ApiJob>(Guid.NewGuid(), start, null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.ScheduleJobFromValuesAsync<ApiJob>(
                jobId: Guid.NewGuid(),
                start: start,
                values: null!,
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECURRING-JOB-COMMANDS", "cancel-run-finalize-use-one-deterministic-identity-and-default-reason")]
    public async Task RecurringCommands_UseTheSameDeterministicIdentityAsync()
    {
        const string JobName = "inventory-refresh";
        Guid expected = RecurringJobIdentity<ApiJob>.CreateId(JobName);
        var cancelEndpoint = new RecordingPublishEndpoint();
        var runEndpoint = new RecordingPublishEndpoint();
        var finalizeEndpoint = new RecordingPublishEndpoint();

        Guid canceled = await cancelEndpoint.CancelRecurringJobAsync<ApiJob>(
            JobName,
            cancellationToken: TestContext.Current.CancellationToken);
        await runEndpoint.RunRecurringJobAsync<ApiJob>(JobName, TestContext.Current.CancellationToken);
        Guid finalized = await finalizeEndpoint.FinalizeRecurringJobAsync<ApiJob>(
            JobName,
            TestContext.Current.CancellationToken);

        CancelJob cancel = Assert.IsAssignableFrom<CancelJob>(cancelEndpoint.Message);
        RunJob run = Assert.IsAssignableFrom<RunJob>(runEndpoint.Message);
        FinalizeJob finalize = Assert.IsAssignableFrom<FinalizeJob>(finalizeEndpoint.Message);
        Assert.Equal(expected, canceled);
        Assert.Equal(expected, finalized);
        Assert.Equal(expected, cancel.JobId);
        Assert.Equal(expected, run.JobId);
        Assert.Equal(expected, finalize.JobId);
        Assert.Equal(JobCancellationReasons.CancellationRequested, cancel.Reason);
    }
}
