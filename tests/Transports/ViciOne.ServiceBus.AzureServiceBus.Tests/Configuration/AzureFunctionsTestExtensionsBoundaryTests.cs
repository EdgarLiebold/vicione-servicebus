using System.Reflection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests.Configuration;

public sealed class AzureFunctionsTestExtensionsBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "public-entry-points-validate-required-inputs")]
    public async Task PublicEntryPoints_RejectEveryMissingRequiredInputAsync()
    {
        ITestHarness harness = DispatchProxy.Create<ITestHarness, EmptyProxy>();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            AzureFunctionsTestExtensions.AddAzureFunctionsTestComponents(null!)).ParamName);
        Assert.Equal("harness", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AzureFunctionsTestExtensions.HandleConsumerAsync<TestConsumer>(null!, new object(), TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            harness.HandleConsumerAsync<TestConsumer>(null!, TestContext.Current.CancellationToken))).ParamName);
    }

    private sealed class TestConsumer : IConsumer;

    private class EmptyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException("The proxy is used only as a non-null test-harness argument.");
    }
}
