using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class AdvancedRequestInitializerExtensionsTests
{
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
