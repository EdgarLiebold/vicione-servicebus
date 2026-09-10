using System.Reflection;
using ViciOne.ServiceBus.Consumer;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

public sealed class InstanceExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-INSTANCE-REGISTRATION", "object-delegate-and-runtime-factory")]
    public async Task EndpointRegistration_UsesTheExactConfiguredInstanceForEveryFactoryShapeAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var objectInstance = new ObjectInstanceConsumer();
        var delegateInstance = new DelegateInstanceConsumer();
        var runtimeFactoryInstance = new RuntimeFactoryConsumer();
        Type? requestedFactoryType = null;
        using var harness = CreateHarness(timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.Instance((object)objectInstance);
            endpoint.Consumer(() => delegateInstance);
            endpoint.Consumer(typeof(RuntimeFactoryConsumer), requestedType =>
            {
                requestedFactoryType = requestedType;
                return runtimeFactoryInstance;
            });
        };

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new ObjectInstanceMessage(), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new DelegateInstanceMessage(), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new RuntimeFactoryMessage(), cancellationToken);

            ObjectInstanceConsumer observedObject = await objectInstance.Consumed.Task.WaitAsync(timeout, cancellationToken);
            DelegateInstanceConsumer observedDelegate = await delegateInstance.Consumed.Task.WaitAsync(timeout, cancellationToken);
            RuntimeFactoryConsumer observedRuntime = await runtimeFactoryInstance.Consumed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Same(objectInstance, observedObject);
            Assert.Same(delegateInstance, observedDelegate);
            Assert.Same(runtimeFactoryInstance, observedRuntime);
            Assert.Equal(typeof(RuntimeFactoryConsumer), requestedFactoryType);
            Assert.Equal(1, objectInstance.ConsumeCount);
            Assert.Equal(1, delegateInstance.ConsumeCount);
            Assert.Equal(1, runtimeFactoryInstance.ConsumeCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-INSTANCE-VALIDATION", "public-boundary-null-inputs")]
    public void PublicInstanceRegistrationAndConnection_RejectNullInputsAtTheirBoundary()
    {
        var instance = new ObjectInstanceConsumer();

        Assert.Throws<ArgumentNullException>(() => InstanceExtensions.Instance(null!, (object)instance));
        Assert.Throws<ArgumentNullException>(() => Bus.Factory.CreateUsingInMemory(configurator =>
            configurator.ReceiveEndpoint($"null-instance-{NewId.NextGuid():N}", endpoint => endpoint.Instance((object)null!))));
        Assert.Throws<ArgumentNullException>(() => InstanceExtensions.ConnectInstance(null!, (object)instance));
        Assert.Equal("consumer", Assert.Throws<ArgumentNullException>(() =>
            new InstanceConsumerFactory<ObjectInstanceConsumer>(null!)).ParamName);
        Assert.Equal("factoryMethod", Assert.Throws<ArgumentNullException>(() =>
            new DelegateConsumerFactory<ObjectInstanceConsumer>(null!)).ParamName);
        Assert.Equal("objectFactory", Assert.Throws<ArgumentNullException>(() =>
            new ObjectConsumerFactory<ObjectInstanceConsumer>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-INSTANCE-VALIDATION", "consumer-factory-dispatch-null-inputs")]
    public async Task ConsumerFactories_RejectNullDispatchArgumentsAtTheirBoundaryAsync()
    {
        ConsumeContext<ObjectInstanceMessage> context =
            DispatchProxy.Create<ConsumeContext<ObjectInstanceMessage>, UnusedConsumeContextProxy>();
        IConsumerFactory<ObjectInstanceConsumer>[] factories =
        [
            new DefaultConstructorConsumerFactory<ObjectInstanceConsumer>(),
            new DelegateConsumerFactory<ObjectInstanceConsumer>(() => new ObjectInstanceConsumer()),
            new InstanceConsumerFactory<ObjectInstanceConsumer>(new ObjectInstanceConsumer()),
            new ObjectConsumerFactory<ObjectInstanceConsumer>(_ => new ObjectInstanceConsumer()),
        ];

        foreach (IConsumerFactory<ObjectInstanceConsumer> factory in factories)
        {
            ArgumentNullException contextException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                factory.SendAsync<ObjectInstanceMessage>(null!, null!));
            Assert.Equal("context", contextException.ParamName);

            ArgumentNullException nextException = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                factory.SendAsync(context, null!));
            Assert.Equal("next", nextException.ParamName);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"instance-registration-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    public sealed record ObjectInstanceMessage;

    public sealed record DelegateInstanceMessage;

    public sealed record RuntimeFactoryMessage;

    private sealed class ObjectInstanceConsumer : IConsumer<ObjectInstanceMessage>
    {
        public TaskCompletionSource<ObjectInstanceConsumer> Consumed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ConsumeCount { get; private set; }

        public Task ConsumeAsync(ConsumeContext<ObjectInstanceMessage> context)
        {
            ConsumeCount++;
            Consumed.TrySetResult(this);
            return Task.CompletedTask;
        }
    }

    private sealed class DelegateInstanceConsumer : IConsumer<DelegateInstanceMessage>
    {
        public TaskCompletionSource<DelegateInstanceConsumer> Consumed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ConsumeCount { get; private set; }

        public Task ConsumeAsync(ConsumeContext<DelegateInstanceMessage> context)
        {
            ConsumeCount++;
            Consumed.TrySetResult(this);
            return Task.CompletedTask;
        }
    }

    private sealed class RuntimeFactoryConsumer : IConsumer<RuntimeFactoryMessage>
    {
        public TaskCompletionSource<RuntimeFactoryConsumer> Consumed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ConsumeCount { get; private set; }

        public Task ConsumeAsync(ConsumeContext<RuntimeFactoryMessage> context)
        {
            ConsumeCount++;
            Consumed.TrySetResult(this);
            return Task.CompletedTask;
        }
    }

    private class UnusedConsumeContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The null-boundary test must not invoke the consume context.");
    }

}
