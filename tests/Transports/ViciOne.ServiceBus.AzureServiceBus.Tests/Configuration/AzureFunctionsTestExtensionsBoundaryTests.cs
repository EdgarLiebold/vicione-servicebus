using System.Reflection;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Testing;
using ViciOne.ServiceBus.Serialization;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "public-sdk-message-and-exact-forwarding")]
    public async Task HandleConsumerAsync_CreatesACompleteSdkMessageAndForwardsTheExactDispatchContextAsync()
    {
        var receiver = new RecordingMessageReceiver();
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IMessageReceiver>(receiver)
            .BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        ITestHarness harness = CreateHarness(scope);
        using var cancellationSource = new CancellationTokenSource();

        await harness.HandleConsumerAsync<TestConsumer>(new TestMessage("payload"), cancellationSource.Token);

        Assert.Equal(DefaultEndpointNameFormatter.Instance.Consumer<TestConsumer>(), receiver.QueueName);
        Assert.NotNull(receiver.Message);
        Assert.Contains("payload", receiver.Message.Body.ToString(), StringComparison.Ordinal);
        Assert.Equal(SystemTextJsonRawMessageSerializer.JsonContentType.MediaType, receiver.Message.ContentType);
        Assert.False(string.IsNullOrWhiteSpace(receiver.Message.MessageId));
        Assert.Equal(1, receiver.Message.DeliveryCount);
        Assert.Equal(cancellationSource.Token, receiver.CancellationToken);
        Assert.Equal(typeof(TestConsumer), receiver.ConsumerType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "pre-cancellation-skips-service-resolution")]
    public async Task HandleConsumerAsync_WithPreCanceledToken_DoesNotResolveAnyServiceAsync()
    {
        ITestHarness harness = DispatchProxy.Create<ITestHarness, EmptyProxy>();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.HandleConsumerAsync<TestConsumer>(new TestMessage("ignored"), cancellationSource.Token));

        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
    }

    private static ITestHarness CreateHarness(IServiceScope scope)
    {
        ITestHarness harness = DispatchProxy.Create<ITestHarness, HarnessProxy>();
        ((HarnessProxy)(object)harness).Scope = scope;
        return harness;
    }

    private sealed class TestConsumer : IConsumer;

    private sealed record TestMessage(string Value);

    private sealed class RecordingMessageReceiver : IMessageReceiver
    {
        public CancellationToken CancellationToken { get; private set; }

        public Type? ConsumerType { get; private set; }

        public ServiceBusReceivedMessage? Message { get; private set; }

        public string? QueueName { get; private set; }

        public void Dispose()
        {
        }

        public Task HandleAsync(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task HandleAsync(string topicPath, string subscriptionName, ServiceBusReceivedMessage message,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task HandleConsumerAsync<TConsumer>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
            where TConsumer : class, IConsumer
        {
            QueueName = queueName;
            Message = message;
            CancellationToken = cancellationToken;
            ConsumerType = typeof(TConsumer);
            return Task.CompletedTask;
        }

        public Task HandleConsumerAsync<TConsumer>(string topicPath, string subscriptionName, ServiceBusReceivedMessage message,
            CancellationToken cancellationToken)
            where TConsumer : class, IConsumer => throw new NotSupportedException();

        public Task HandleSagaAsync<TSaga>(string queueName, ServiceBusReceivedMessage message, CancellationToken cancellationToken)
            where TSaga : class, ISaga => throw new NotSupportedException();

        public Task HandleSagaAsync<TSaga>(string topicPath, string subscriptionName, ServiceBusReceivedMessage message,
            CancellationToken cancellationToken)
            where TSaga : class, ISaga => throw new NotSupportedException();

        public Task HandleExecuteActivityAsync<TActivity>(string queueName, ServiceBusReceivedMessage message,
            CancellationToken cancellationToken)
            where TActivity : class => throw new NotSupportedException();
    }

    private class HarnessProxy : DispatchProxy
    {
        public IServiceScope Scope { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Scope")
                return Scope;

            throw new NotSupportedException($"The test harness member '{targetMethod?.Name}' is not used by this test.");
        }
    }

    private class EmptyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException("The proxy is used only as a non-null test-harness argument.");
    }
}
