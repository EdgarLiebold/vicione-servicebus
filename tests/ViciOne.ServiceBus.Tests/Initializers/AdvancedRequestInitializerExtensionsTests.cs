using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class AdvancedRequestInitializerExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-REQUEST-INITIALIZER", "exact-overload-forwarding")]
    public async Task InitializerOverloads_ForwardExactValuesCallbackTimeoutAndCancellationAsync()
    {
        IRecordingRequestClient client = DispatchProxy.Create<IRecordingRequestClient, RecordingProxy>();
        var proxy = (RecordingProxy)(object)client;
        proxy.ThrowOnInvocation = false;
        var values = new { Value = "request" };
        RequestPipeConfiguratorCallback<TestRequest> callback = _ => { };
        var timeout = new RequestTimeout(TimeSpan.FromSeconds(7));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        _ = AdvancedRequestInitializerExtensions.Create(client, values, timeout, cancellationToken);
        _ = await AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse>(
            client, values, timeout, cancellationToken);
        _ = await AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse>(
            client, values, callback, timeout, cancellationToken);
        _ = await AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(
            client, values, timeout, cancellationToken);
        _ = await AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(
            client, values, callback, timeout, cancellationToken);
        _ = await AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(
            client, values, timeout, cancellationToken);
        _ = await AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(
            client, values, callback, timeout, cancellationToken);

        Assert.Equal(7, proxy.InvocationCount);
        AssertInvocation(proxy.Invocations[0], values, null, timeout, cancellationToken, []);
        AssertInvocation(proxy.Invocations[1], values, null, timeout, cancellationToken, [typeof(FirstResponse)]);
        AssertInvocation(proxy.Invocations[2], values, callback, timeout, cancellationToken, [typeof(FirstResponse)]);
        AssertInvocation(proxy.Invocations[3], values, null, timeout, cancellationToken, [typeof(FirstResponse), typeof(SecondResponse)]);
        AssertInvocation(proxy.Invocations[4], values, callback, timeout, cancellationToken, [typeof(FirstResponse), typeof(SecondResponse)]);
        AssertInvocation(proxy.Invocations[5], values, null, timeout, cancellationToken,
            [typeof(FirstResponse), typeof(SecondResponse), typeof(ThirdResponse)]);
        AssertInvocation(proxy.Invocations[6], values, callback, timeout, cancellationToken,
            [typeof(FirstResponse), typeof(SecondResponse), typeof(ThirdResponse)]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-REQUEST-INITIALIZER", "capability-validation")]
    public void InitializerOverloads_RejectMissingAndUnsupportedClients()
    {
        var values = new { Value = "request" };
        IRequestClient<TestRequest> basicClient = DispatchProxy.Create<IRequestClient<TestRequest>, RecordingProxy>();
        RequestPipeConfiguratorCallback<TestRequest> callback = _ => { };
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        AssertNull("client", () => AdvancedRequestInitializerExtensions.Create<TestRequest>(
            null!, values, cancellationToken: cancellationToken));
        AssertNull("client", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse>(
            null!, values, cancellationToken: cancellationToken));
        AssertNull("client", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse>(
            null!, values, callback, cancellationToken: cancellationToken));
        AssertNull("client", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(
            null!, values, cancellationToken: cancellationToken));
        AssertNull("client", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(
            null!, values, callback, cancellationToken: cancellationToken));
        AssertNull("client", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(
            null!, values, cancellationToken: cancellationToken));
        AssertNull("client", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(
            null!, values, callback, cancellationToken: cancellationToken));
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedRequestInitializerExtensions.Create(basicClient, values, cancellationToken: cancellationToken);
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse>(
                basicClient, values, cancellationToken: cancellationToken);
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse>(
                basicClient, values, callback, cancellationToken: cancellationToken);
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(
                basicClient, values, cancellationToken: cancellationToken);
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(
                basicClient, values, callback, cancellationToken: cancellationToken);
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(
                basicClient, values, cancellationToken: cancellationToken);
        });
        Assert.Throws<NotSupportedException>(() =>
        {
            _ = AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(
                basicClient, values, callback, cancellationToken: cancellationToken);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-REQUEST-INITIALIZER", "required-inputs-before-provider-use")]
    public void InitializerOverloads_RejectEveryMissingRequiredInputBeforeProviderUse()
    {
        IRecordingRequestClient client = DispatchProxy.Create<IRecordingRequestClient, RecordingProxy>();
        var values = new { Value = "request" };

        AssertNull("values", () => AdvancedRequestInitializerExtensions.Create(client, null!));
        AssertNull("values", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse>(client, null!));
        AssertNull("values", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse>(client, null!, _ => { }));
        AssertNull("values", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(client, null!));
        AssertNull("values", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(client, null!, _ => { }));
        AssertNull("values", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(client, null!));
        AssertNull("values", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(client, null!, _ => { }));

        AssertNull("callback", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse>(client, values, null!));
        AssertNull("callback", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(client, values, null!));
        AssertNull("callback", () => AdvancedRequestInitializerExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(client, values, null!));

        Assert.Equal(0, ((RecordingProxy)(object)client).InvocationCount);
    }

    public interface IRecordingRequestClient :
        IRequestClient<TestRequest>,
        IAdvancedRequestClient<TestRequest>
    {
    }

    public sealed record TestRequest(string Value);

    private sealed record FirstResponse;

    private sealed record SecondResponse;

    private sealed record ThirdResponse;

    public class RecordingProxy : DispatchProxy
    {
        public List<Invocation> Invocations { get; } = [];

        public int InvocationCount => Invocations.Count;

        public bool ThrowOnInvocation { get; set; } = true;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            Invocations.Add(new Invocation(targetMethod, args ?? []));
            if (ThrowOnInvocation)
                throw new InvalidOperationException($"The provider must not be invoked ({targetMethod.Name}).");

            if (!targetMethod.ReturnType.IsGenericType || targetMethod.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                return null;

            Type resultType = targetMethod.ReturnType.GetGenericArguments()[0];
            MethodInfo factory = typeof(RecordingProxy)
                .GetMethod(nameof(CompletedTaskAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(resultType);
            return factory.Invoke(null, null);
        }

        static Task<TResult?> CompletedTaskAsync<TResult>() => Task.FromResult<TResult?>(default);
    }

    public sealed record Invocation(MethodInfo Method, object?[] Arguments);

    private static void AssertInvocation(Invocation invocation, object values,
        RequestPipeConfiguratorCallback<TestRequest>? callback, RequestTimeout timeout,
        CancellationToken cancellationToken, Type[] responseTypes)
    {
        Assert.Equal(responseTypes, invocation.Method.GetGenericArguments());
        Assert.Equal(typeof(object), invocation.Method.GetParameters()[0].ParameterType);
        Assert.Same(values, invocation.Arguments[0]);

        int timeoutIndex;
        if (callback is null)
            timeoutIndex = 1;
        else
        {
            Assert.Same(callback, invocation.Arguments[1]);
            timeoutIndex = 2;
        }

        Assert.Equal(timeout, invocation.Arguments[timeoutIndex]);
        Assert.Equal(cancellationToken, invocation.Arguments[timeoutIndex + 1]);
    }

    private static void AssertNull(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);
}
