using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.Tests.JobService.Api;

internal sealed class RecordingSubmitJobClient<TJob> : IRequestClient<SubmitJob<TJob>>
    where TJob : class
{
    public SubmitJob<TJob>? Request { get; private set; }
    public CancellationToken CancellationToken { get; private set; }

    public Task<Response<TResponse>> GetResponseAsync<TResponse>(
        SubmitJob<TJob> request,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        Request = request;
        CancellationToken = cancellationToken;

        if (typeof(TResponse) != typeof(JobSubmissionAccepted))
            throw new NotSupportedException($"Response type '{typeof(TResponse)}' is not supported by this test client.");

        var accepted = new JobSubmissionAcceptedResponse { JobId = request.JobId };
        return Task.FromResult(ResponseFactory.Create((TResponse)(object)accepted));
    }

    public Task<Response<TResponse>> GetResponseAsync<TResponse>(
        SubmitJob<TJob> request,
        RequestOptions options,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(options);
        return GetResponseAsync<TResponse>(request, cancellationToken);
    }
}
