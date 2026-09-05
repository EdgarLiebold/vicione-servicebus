using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class GenericRequestClientTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-REQUEST-OPTIONS", "scoped-client-wrapper-forwards-exact-call")]
    public void OptionsOverload_ForwardsTheExactMessageOptionsTokenAndTask()
    {
        Response<TestResponse> response = DispatchProxy.Create<Response<TestResponse>, UnusedResponseProxy>();
        Task<Response<TestResponse>> responseTask = Task.FromResult(response);
        var inner = new RecordingRequestClient(responseTask);
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IScopedClientFactory>(new RecordingScopedClientFactory(inner))
            .BuildServiceProvider();
        IRequestClient<TestRequest> client = new GenericRequestClient<TestRequest>(provider);
        var request = new TestRequest("order-42");
        var options = new RequestOptions
        {
            Headers = new Dictionary<string, object?> { ["tenant"] = "north" },
            RequestId = Guid.Parse("11000000-0000-0000-0000-000000000011"),
        };
        using var cancellation = new CancellationTokenSource();

        Task<Response<TestResponse>> actual = client.GetResponseAsync<TestResponse>(
            request,
            options,
            cancellation.Token);

        Assert.Same(responseTask, actual);
        Assert.Same(request, inner.Request);
        Assert.Same(options, inner.Options);
        Assert.Equal(cancellation.Token, inner.CancellationToken);
    }

    private sealed record TestRequest(string Value);

    private sealed record TestResponse(string Value);

    private sealed class RecordingRequestClient(Task<Response<TestResponse>> response) : IRequestClient<TestRequest>
    {
        public TestRequest? Request { get; private set; }

        public RequestOptions? Options { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<Response<TResponse>> GetResponseAsync<TResponse>(
            TestRequest request,
            CancellationToken cancellationToken = default)
            where TResponse : class => throw new NotSupportedException();

        public Task<Response<TResponse>> GetResponseAsync<TResponse>(
            TestRequest request,
            RequestOptions options,
            CancellationToken cancellationToken = default)
            where TResponse : class
        {
            Request = request;
            Options = options;
            CancellationToken = cancellationToken;
            return (Task<Response<TResponse>>)(object)response;
        }
    }

    private sealed class RecordingScopedClientFactory(IRequestClient<TestRequest> client) : IScopedClientFactory
    {
        public RequestHandle<T> CreateRequest<T>(
            T message,
            CancellationToken cancellationToken = default,
            RequestTimeout timeout = default)
            where T : class => throw new NotSupportedException();

        public RequestHandle<T> CreateRequest<T>(
            Uri destinationAddress,
            T message,
            CancellationToken cancellationToken = default,
            RequestTimeout timeout = default)
            where T : class => throw new NotSupportedException();

        public RequestHandle<T> CreateRequest<T>(
            object values,
            CancellationToken cancellationToken = default,
            RequestTimeout timeout = default)
            where T : class => throw new NotSupportedException();

        public RequestHandle<T> CreateRequest<T>(
            Uri destinationAddress,
            object values,
            CancellationToken cancellationToken = default,
            RequestTimeout timeout = default)
            where T : class => throw new NotSupportedException();

        public IRequestClient<T> CreateRequestClient<T>(RequestTimeout timeout = default)
            where T : class => (IRequestClient<T>)(object)client;

        public IRequestClient<T> CreateRequestClient<T>(Uri destinationAddress, RequestTimeout timeout = default)
            where T : class => throw new NotSupportedException();
    }

    private class UnusedResponseProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The response must not be observed by the forwarding test ({targetMethod?.Name}).");
    }
}
