using System.Reflection;
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
        Assert.Equal(typeof(ISubmitJob<ApiJob>), endpoint.ContractType);
        ISubmitJob<ApiJob> command = Assert.IsAssignableFrom<ISubmitJob<ApiJob>>(endpoint.Message);
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
        ISubmitJob<ApiJob> command = Assert.IsAssignableFrom<ISubmitJob<ApiJob>>(client.Request);
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
    [RequirementCoverage("REQ-VSB-JOB-SUBMISSION-API", "every-generated-id-property-and-values-overload-forwards-complete-state")]
    public async Task GeneratedSubmissionOverloads_ForwardJobsValuesPropertiesAndAcceptedIdentityAsync()
    {
        using var cancellation = new CancellationTokenSource();

        var typedPublish = new RecordingPublishEndpoint();
        Guid typedPublishId = await typedPublish.SubmitJobAsync(
            new ApiJob { Label = "typed-publish" },
            properties => properties.Set("tenant", "north"),
            cancellation.Token);
        ISubmitJob<ApiJob> typedPublishCommand = Assert.IsAssignableFrom<ISubmitJob<ApiJob>>(typedPublish.Message);
        Assert.NotEqual(Guid.Empty, typedPublishId);
        Assert.Equal(typedPublishId, typedPublishCommand.JobId);
        Assert.Equal("north", typedPublishCommand.JobProperties?["tenant"]);

        var valuesPublish = new RecordingPublishEndpoint();
        Guid valuesPublishId = await valuesPublish.SubmitJobFromValuesAsync<ApiJob>(
            new { Label = "values-publish" },
            cancellation.Token);
        ISubmitJob<ApiJob> valuesPublishCommand = Assert.IsAssignableFrom<ISubmitJob<ApiJob>>(valuesPublish.Message);
        Assert.Equal(valuesPublishId, valuesPublishCommand.JobId);
        Assert.Equal("values-publish", valuesPublishCommand.Job.Label);

        var valuesPropertiesPublish = new RecordingPublishEndpoint();
        Guid valuesPropertiesPublishId = await valuesPropertiesPublish.SubmitJobFromValuesAsync<ApiJob>(
            new { Label = "values-properties-publish" },
            properties => properties.Set("priority", 7),
            cancellation.Token);
        ISubmitJob<ApiJob> valuesPropertiesPublishCommand =
            Assert.IsAssignableFrom<ISubmitJob<ApiJob>>(valuesPropertiesPublish.Message);
        Assert.Equal(valuesPropertiesPublishId, valuesPropertiesPublishCommand.JobId);
        Assert.Equal(7, valuesPropertiesPublishCommand.JobProperties?["priority"]);

        var typedRequest = new RecordingSubmitJobClient<ApiJob>();
        Guid typedRequestId = await typedRequest.SubmitJobAsync(
            new ApiJob { Label = "typed-request" },
            cancellation.Token);
        Assert.Equal(typedRequest.Request?.JobId, typedRequestId);

        var typedPropertiesRequest = new RecordingSubmitJobClient<ApiJob>();
        Guid typedPropertiesRequestId = await typedPropertiesRequest.SubmitJobAsync(
            new ApiJob { Label = "typed-properties-request" },
            properties => properties.Set("tenant", "south"),
            cancellation.Token);
        Assert.Equal(typedPropertiesRequest.Request?.JobId, typedPropertiesRequestId);
        Assert.Equal("south", typedPropertiesRequest.Request?.JobProperties?["tenant"]);

        var valuesRequest = new RecordingSubmitJobClient<ApiJob>();
        Guid valuesRequestId = await valuesRequest.SubmitJobFromValuesAsync<ApiJob>(
            new { Label = "values-request" },
            cancellation.Token);
        Assert.Equal(valuesRequest.Request?.JobId, valuesRequestId);
        Assert.Equal("values-request", valuesRequest.Request?.Job.Label);

        var valuesPropertiesRequest = new RecordingSubmitJobClient<ApiJob>();
        Guid valuesPropertiesRequestId = await valuesPropertiesRequest.SubmitJobFromValuesAsync<ApiJob>(
            new { Label = "values-properties-request" },
            properties => properties.Set("priority", 11),
            cancellation.Token);
        Assert.Equal(valuesPropertiesRequest.Request?.JobId, valuesPropertiesRequestId);
        Assert.Equal(11, valuesPropertiesRequest.Request?.JobProperties?["priority"]);

        IRecordingDirectJobClient direct = DispatchProxy.Create<IRecordingDirectJobClient, DirectJobClientProxy>();
        var directProxy = (DirectJobClientProxy)(object)direct;
        Guid acceptedTypedId = NewId.NextGuid();
        directProxy.AcceptedJobId = acceptedTypedId;
        var directJob = new ApiJob { Label = "direct-typed" };

        Assert.Equal(acceptedTypedId, await direct.SubmitJobAsync(directJob, cancellation.Token));
        Assert.Same(directJob, directProxy.Request);
        Assert.Equal(cancellation.Token, directProxy.CancellationToken);

        Guid acceptedValuesId = NewId.NextGuid();
        directProxy.AcceptedJobId = acceptedValuesId;
        var directValues = new { Label = "direct-values" };
        Assert.Equal(acceptedValuesId, await direct.SubmitJobFromValuesAsync<ApiJob>(directValues, cancellation.Token));
        Assert.Same(directValues, directProxy.Request);
        Assert.Equal(cancellation.Token, directProxy.CancellationToken);
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
        Assert.Equal("setJobProperties", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SubmitJobAsync(new ApiJob(), null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("setJobProperties", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            endpoint.SubmitJobFromValuesAsync<ApiJob>(new { Label = "job" }, null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("setJobProperties", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.SubmitJobAsync(new ApiJob(), null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("setJobProperties", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.SubmitJobFromValuesAsync<ApiJob>(new { Label = "job" }, null!, TestContext.Current.CancellationToken))).ParamName);

        IRecordingDirectJobClient direct = DispatchProxy.Create<IRecordingDirectJobClient, DirectJobClientProxy>();
        Assert.Equal("job", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            direct.SubmitJobAsync<ApiJob>(null!, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            direct.SubmitJobFromValuesAsync<ApiJob>(null!, TestContext.Current.CancellationToken))).ParamName);
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

        ICancelJob command = Assert.IsAssignableFrom<ICancelJob>(endpoint.Message);
        Assert.Equal(jobId, command.JobId);
        Assert.Equal(JobCancellationReasons.CancellationRequested, command.Reason);
    }

    private interface IRecordingDirectJobClient : IRequestClient<ApiJob>, IAdvancedRequestClient<ApiJob>;

    private class DirectJobClientProxy : DispatchProxy
    {
        public Guid AcceptedJobId { get; set; }

        public object? Request { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name != nameof(IRequestClient<ApiJob>.GetResponseAsync)
                || targetMethod.GetGenericArguments() is not [Type responseType]
                || responseType != typeof(IJobSubmissionAccepted))
                throw new NotSupportedException(targetMethod.Name);

            Request = args![0];
            CancellationToken = args.OfType<CancellationToken>().Single();
            var accepted = new JobSubmissionAcceptedResponse { JobId = AcceptedJobId };
            return Task.FromResult(ResponseFactory.Create<IJobSubmissionAccepted>(accepted));
        }
    }
}
