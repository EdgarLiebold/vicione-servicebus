using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Api;

public sealed class JobServiceExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SUBMISSION-API", "typed-publish-preserves-id-job-properties-and-token")]
    public async Task SubmitJobAsync_TypedPublishPreservesTheCompleteCommandAsync()
    {
        var endpoint = new RecordingPublishEndpoint();
        var job = new ApiJob { Label = "typed" };
        Guid jobId = NewId.NextGuid();
        using var cancellation = new CancellationTokenSource();

        Guid result = await endpoint.SubmitJobAsync(
            jobId,
            job,
            properties => properties.Set("tenant", "north"),
            cancellation.Token);

        Assert.Equal(jobId, result);
        Assert.Equal(typeof(SubmitJob<ApiJob>), endpoint.ContractType);
        SubmitJob<ApiJob> command = Assert.IsAssignableFrom<SubmitJob<ApiJob>>(endpoint.Message);
        Assert.Same(job, command.Job);
        Assert.Equal(jobId, command.JobId);
        IReadOnlyDictionary<string, object> properties = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(command.JobProperties);
        Assert.Equal("north", properties["tenant"]);
        Assert.Equal(cancellation.Token, endpoint.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SUBMISSION-API", "values-request-initializes-contract-and-preserves-id-properties-token")]
    public async Task SubmitJobFromValuesAsync_RequestInitializesTheTypedContractAsync()
    {
        var client = new RecordingSubmitJobClient<ApiJob>();
        Guid jobId = NewId.NextGuid();
        using var cancellation = new CancellationTokenSource();

        Guid result = await client.SubmitJobFromValuesAsync<ApiJob>(
            jobId,
            new { Label = "initialized" },
            properties => properties.Set("priority", 7),
            cancellation.Token);

        Assert.Equal(jobId, result);
        SubmitJob<ApiJob> command = Assert.IsAssignableFrom<SubmitJob<ApiJob>>(client.Request);
        Assert.Equal("initialized", command.Job.Label);
        IReadOnlyDictionary<string, object> properties = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(command.JobProperties);
        Assert.Equal(7, properties["priority"]);
        Assert.Equal(cancellation.Token, client.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SUBMISSION-API", "generated-identities-are-nonempty-and-distinct")]
    public async Task SubmitJobAsync_GeneratedIdentifiersAreNonEmptyAndDistinctAsync()
    {
        var firstEndpoint = new RecordingPublishEndpoint();
        var secondEndpoint = new RecordingPublishEndpoint();

        Guid first = await firstEndpoint.SubmitJobAsync(
            new ApiJob { Label = "first" },
            TestContext.Current.CancellationToken);
        Guid second = await secondEndpoint.SubmitJobAsync(
            new ApiJob { Label = "second" },
            TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, first);
        Assert.NotEqual(Guid.Empty, second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SUBMISSION-API", "null-job-and-values-boundaries")]
    public async Task SubmissionApis_RejectNullJobsAndInitializerValuesAsync()
    {
        var endpoint = new RecordingPublishEndpoint();
        var client = new RecordingSubmitJobClient<ApiJob>();

        Assert.Equal("job", (await Assert.ThrowsAsync<ArgumentNullException>(
            () => endpoint.SubmitJobAsync<ApiJob>(Guid.NewGuid(), null!, cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(
            () => endpoint.SubmitJobFromValuesAsync<ApiJob>(
                jobId: Guid.NewGuid(),
                values: null!,
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("job", (await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.SubmitJobAsync(Guid.NewGuid(), null!, cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.SubmitJobFromValuesAsync<ApiJob>(
                jobId: Guid.NewGuid(),
                values: null!,
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SUBMISSION-API", "empty-explicit-identities-are-rejected")]
    public async Task SubmissionApis_RejectEmptyExplicitIdentifiersAsync()
    {
        var endpoint = new RecordingPublishEndpoint();
        var client = new RecordingSubmitJobClient<ApiJob>();
        var job = new ApiJob();

        Assert.Equal("jobId", (await Assert.ThrowsAsync<ArgumentException>(
            () => endpoint.SubmitJobAsync(Guid.Empty, job, cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("jobId", (await Assert.ThrowsAsync<ArgumentException>(
            () => endpoint.SubmitJobFromValuesAsync<ApiJob>(
                Guid.Empty,
                new { Label = "job" },
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("jobId", (await Assert.ThrowsAsync<ArgumentException>(
            () => client.SubmitJobAsync(Guid.Empty, job, cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("jobId", (await Assert.ThrowsAsync<ArgumentException>(
            () => client.SubmitJobFromValuesAsync<ApiJob>(
                Guid.Empty,
                new { Label = "job" },
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-COMMAND-API", "empty-identities-and-null-endpoints-are-rejected")]
    public async Task LifecycleCommandApis_RejectInvalidRequiredArgumentsAsync()
    {
        var endpoint = new RecordingPublishEndpoint();

        Assert.Equal("publishEndpoint", (await Assert.ThrowsAsync<ArgumentNullException>(
            () => JobServiceExtensions.CancelJobAsync(
                null!,
                Guid.NewGuid(),
                cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("jobId", (await Assert.ThrowsAsync<ArgumentException>(
            () => endpoint.CancelJobAsync(Guid.Empty, cancellationToken: TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("jobId", (await Assert.ThrowsAsync<ArgumentException>(
            () => endpoint.RetryJobAsync(Guid.Empty, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("jobId", (await Assert.ThrowsAsync<ArgumentException>(
            () => endpoint.FinalizeJobAsync(Guid.Empty, TestContext.Current.CancellationToken))).ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-JOB-COMMAND-API", "missing-cancellation-reason-uses-domain-default")]
    public async Task CancelJobAsync_MissingReasonUsesTheDomainDefaultAsync(string? reason)
    {
        var endpoint = new RecordingPublishEndpoint();
        Guid jobId = NewId.NextGuid();

        await endpoint.CancelJobAsync(jobId, reason, TestContext.Current.CancellationToken);

        CancelJob command = Assert.IsAssignableFrom<CancelJob>(endpoint.Message);
        Assert.Equal(jobId, command.JobId);
        Assert.Equal(JobCancellationReasons.CancellationRequested, command.Reason);
    }
}
