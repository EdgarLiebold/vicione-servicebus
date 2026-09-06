using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class GenericRequestClientTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-FACTORY-FORWARDING", "typed-message-timeout-and-token")]
    public void TypedCreate_ForwardsMessageTimeoutAndCancellationTokenInContractOrder()
    {
        (IRequestClient<TestRequest> inner, RecordingAdvancedRequestClientProxy recorder) = CreateAdvancedClientRecorder();
        IRequestClient<TestRequest> client = CreateGenericClient(inner);
        var request = new TestRequest("typed-request");
        RequestTimeout timeout = RequestTimeout.After(s: 23);
        using var cancellation = new CancellationTokenSource();

        RequestHandle<TestRequest> actual = client.Advanced().Create(request, timeout, cancellation.Token);

        Assert.Same(recorder.ReturnedHandle, actual);
        Assert.Equal(nameof(IAdvancedRequestClient<TestRequest>.Create), recorder.InvokedMethod?.Name);
        Assert.Same(request, recorder.Arguments?[0]);
        Assert.Equal(timeout, recorder.Arguments?[1]);
        Assert.Equal(cancellation.Token, recorder.Arguments?[2]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-FACTORY-FORWARDING", "initializer-values-timeout-and-token")]
    public void InitializerCreate_ForwardsValuesTimeoutAndCancellationTokenInContractOrder()
    {
        (IRequestClient<TestRequest> inner, RecordingAdvancedRequestClientProxy recorder) = CreateAdvancedClientRecorder();
        IRequestClient<TestRequest> client = CreateGenericClient(inner);
        var values = new { Value = "initialized-request" };
        RequestTimeout timeout = RequestTimeout.After(s: 29);
        using var cancellation = new CancellationTokenSource();

        RequestHandle<TestRequest> actual = client.Advanced().Create(values, timeout, cancellation.Token);

        Assert.Same(recorder.ReturnedHandle, actual);
        Assert.Equal(nameof(IAdvancedRequestClient<TestRequest>.Create), recorder.InvokedMethod?.Name);
        Assert.Same(values, recorder.Arguments?[0]);
        Assert.Equal(timeout, recorder.Arguments?[1]);
        Assert.Equal(cancellation.Token, recorder.Arguments?[2]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-REQUEST-OPTIONS", "scoped-client-wrapper-forwards-exact-call")]
    public void OptionsOverload_ForwardsTheExactMessageOptionsTokenAndTask()
    {
        Response<TestResponse> response = DispatchProxy.Create<Response<TestResponse>, UnusedResponseProxy>();
        Task<Response<TestResponse>> responseTask = Task.FromResult(response);
        var inner = new RecordingRequestClient(responseTask);
        IRequestClient<TestRequest> client = CreateGenericClient(inner);
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

    public sealed record TestRequest(string Value);

    private sealed record TestResponse(string Value);

    public interface IRecordingAdvancedRequestClient :
        IRequestClient<TestRequest>,
        IAdvancedRequestClient<TestRequest>
    {
    }

    private static IRequestClient<TestRequest> CreateGenericClient(IRequestClient<TestRequest> inner)
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IScopedClientFactory>(new RecordingScopedClientFactory(inner))
            .BuildServiceProvider();
        return new GenericRequestClient<TestRequest>(provider);
    }

    private static (IRequestClient<TestRequest>, RecordingAdvancedRequestClientProxy) CreateAdvancedClientRecorder()
    {
        IRecordingAdvancedRequestClient client = DispatchProxy.Create<IRecordingAdvancedRequestClient, RecordingAdvancedRequestClientProxy>();
        var recorder = (RecordingAdvancedRequestClientProxy)(object)client;
        recorder.ReturnedHandle = DispatchProxy.Create<RequestHandle<TestRequest>, PassiveRequestHandleProxy>();
        return (client, recorder);
    }

    public class RecordingAdvancedRequestClientProxy : DispatchProxy
    {
        public MethodInfo? InvokedMethod { get; private set; }

        public object?[]? Arguments { get; private set; }

        public RequestHandle<TestRequest> ReturnedHandle { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            InvokedMethod = targetMethod;
            Arguments = args;

            return targetMethod?.Name == nameof(IAdvancedRequestClient<TestRequest>.Create)
                ? ReturnedHandle
                : throw new InvalidOperationException($"Unexpected forwarded method: {targetMethod?.Name}.");
        }
    }

    public class PassiveRequestHandleProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The returned request handle must remain untouched ({targetMethod?.Name}).");
    }

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
            RequestTimeout timeout = default,
            CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException();

        public RequestHandle<T> CreateRequest<T>(
            Uri destinationAddress,
            T message,
            RequestTimeout timeout = default,
            CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException();

        public RequestHandle<T> CreateRequest<T>(
            object values,
            RequestTimeout timeout = default,
            CancellationToken cancellationToken = default)
            where T : class => throw new NotSupportedException();

        public RequestHandle<T> CreateRequest<T>(
            Uri destinationAddress,
            object values,
            RequestTimeout timeout = default,
            CancellationToken cancellationToken = default)
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
