using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced;

public sealed class AdvancedRequestClientExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-REQUEST", "capability-validation")]
    public void Advanced_RejectsMissingAndUnsupportedRequestClients()
    {
        IRequestClient<TestRequest>? missing = null;
        IRequestClient<TestRequest> unsupported = DispatchProxy.Create<IRequestClient<TestRequest>, RecordingProxy>();

        Assert.Equal(
            "client",
            Assert.Throws<ArgumentNullException>(() => AdvancedRequestClientExtensions.Advanced(missing!)).ParamName);
        Assert.Throws<NotSupportedException>(() => unsupported.Advanced());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ADVANCED-REQUEST", "required-inputs-before-provider-use")]
    public void RequestOverloads_RejectEveryMissingRequiredInputBeforeProviderUse()
    {
        IRecordingRequestClient client = DispatchProxy.Create<IRecordingRequestClient, RecordingProxy>();
        var request = new TestRequest("request");

        AssertNull("request", () => AdvancedRequestClientExtensions.Create(client, null!));
        AssertNull("request", () => AdvancedRequestClientExtensions.GetResponseAsync<TestRequest, FirstResponse>(
            client, null!, default(RequestTimeout)));
        AssertNull("request", () => AdvancedRequestClientExtensions.GetResponseAsync<TestRequest, FirstResponse>(
            client, null!, _ => { }));
        AssertNull("request", () => AdvancedRequestClientExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(
            client, null!));
        AssertNull("request", () => AdvancedRequestClientExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(
            client, null!, _ => { }));
        AssertNull("request", () => AdvancedRequestClientExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(
            client, null!));
        AssertNull("request", () => AdvancedRequestClientExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(
            client, null!, _ => { }));

        AssertNull("callback", () => AdvancedRequestClientExtensions.GetResponseAsync<TestRequest, FirstResponse>(
            client, request, null!));
        AssertNull("callback", () => AdvancedRequestClientExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse>(
            client, request, null!));
        AssertNull("callback", () => AdvancedRequestClientExtensions.GetResponseAsync<TestRequest, FirstResponse, SecondResponse, ThirdResponse>(
            client, request, null!));

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
        public int InvocationCount { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            InvocationCount++;
            throw new InvalidOperationException($"The provider must not be invoked ({targetMethod?.Name}).");
        }
    }

    private static void AssertNull(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);
}
