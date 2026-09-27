using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Sagas.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class MessageReceiverIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "same-path-selects-requested-consumer")]
    public async Task SamePath_DispatchesOnlyToTheRequestedConsumerAsync(bool subscription)
    {
        var deliveries = new Deliveries();
        await using ServiceProvider provider = CreateProvider(deliveries);
        using var receiver = new MessageReceiver(provider.GetRequiredService<IBusRegistrationContext>(),
            new PassiveBusHandle(), provider.GetRequiredService<IBusInstance>());

        await DispatchAsync<FirstConsumer>("first");
        Assert.Equal(["A:first"], deliveries.Values.ToArray());
        await DispatchAsync<SecondConsumer>("second");
        Assert.Equal(["A:first", "B:second"], deliveries.Values.ToArray());
        await DispatchAsync<FirstConsumer>("third");
        Assert.Equal(["A:first", "B:second", "A:third"], deliveries.Values.ToArray());

        Task DispatchAsync<TConsumer>(string value) where TConsumer : class, IConsumer
        {
            ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromObjectAsJson(new Payload(value)), messageId: value,
                contentType: "application/json", deliveryCount: 1);
            return subscription
                ? receiver.HandleConsumerAsync<TConsumer>("shared-topic", "shared-subscription", message,
                    TestContext.Current.CancellationToken)
                : receiver.HandleConsumerAsync<TConsumer>("shared-queue", message, TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "typed-and-all-pipelines-are-independent")]
    public async Task TypedAndAllDispatch_KeepTheirConsumerSetsIndependentAsync(bool subscription, bool allFirst)
    {
        var deliveries = new Deliveries();
        await using ServiceProvider provider = CreateProvider(deliveries);
        using var receiver = new MessageReceiver(provider.GetRequiredService<IBusRegistrationContext>(),
            new PassiveBusHandle(), provider.GetRequiredService<IBusInstance>());

        if (allFirst)
        {
            await DispatchAllAsync("all");
            Assert.Equal(["A:all", "B:all"], deliveries.Values.Order().ToArray());
            await DispatchTypedAsync();
        }
        else
        {
            await DispatchTypedAsync();
            Assert.Equal(["A:typed"], deliveries.Values.ToArray());
            await DispatchAllAsync("all");
        }

        Assert.Equal(["A:all", "A:typed", "B:all"], deliveries.Values.Order().ToArray());
        await DispatchAllAsync("repeat");
        Assert.Equal(["A:all", "A:repeat", "A:typed", "B:all", "B:repeat"], deliveries.Values.Order().ToArray());

        Task DispatchTypedAsync() => subscription
            ? receiver.HandleConsumerAsync<FirstConsumer>("shared-topic", "shared-subscription", Message("typed"), TestContext.Current.CancellationToken)
            : receiver.HandleConsumerAsync<FirstConsumer>("shared-queue", Message("typed"), TestContext.Current.CancellationToken);

        Task DispatchAllAsync(string value) => subscription
            ? receiver.HandleAsync("shared-topic", "shared-subscription", Message(value), TestContext.Current.CancellationToken)
            : receiver.HandleAsync("shared-queue", Message(value), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "each-path-configures-all-consumers")]
    public async Task DifferentPaths_ConfigureAllConsumersForEachReceiverAsync(bool subscription)
    {
        var deliveries = new Deliveries();
        await using ServiceProvider provider = CreateProvider(deliveries);
        using var receiver = new MessageReceiver(provider.GetRequiredService<IBusRegistrationContext>(),
            new PassiveBusHandle(), provider.GetRequiredService<IBusInstance>());

        await DispatchAsync("first", "one");
        Assert.Equal(["A:one", "B:one"], deliveries.Values.Order().ToArray());
        await DispatchAsync("second", "two");
        Assert.Equal(["A:one", "A:two", "B:one", "B:two"], deliveries.Values.Order().ToArray());

        Task DispatchAsync(string path, string value) => subscription
            ? receiver.HandleAsync("shared-topic", path, Message(value), TestContext.Current.CancellationToken)
            : receiver.HandleAsync(path, Message(value), TestContext.Current.CancellationToken);
    }

    private static ServiceBusReceivedMessage Message(string value) => ServiceBusModelFactory.ServiceBusReceivedMessage(
        body: BinaryData.FromObjectAsJson(new Payload(value)), messageId: Guid.NewGuid().ToString("D"),
        contentType: "application/json", deliveryCount: 1);

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "invalid-subscription-cannot-alias-queue")]
    public async Task InvalidSubscription_CannotReuseAnExistingQueuePipelineAsync()
    {
        var deliveries = new Deliveries();
        await using ServiceProvider provider = CreateProvider(deliveries);
        using var receiver = new MessageReceiver(provider.GetRequiredService<IBusRegistrationContext>(),
            new PassiveBusHandle(), provider.GetRequiredService<IBusInstance>());

        await receiver.HandleConsumerAsync<FirstConsumer>("shared-topic", Message("queue"), TestContext.Current.CancellationToken);
        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() => receiver.HandleConsumerAsync<FirstConsumer>(
            "shared-topic", null!, Message("invalid"), TestContext.Current.CancellationToken));
        Assert.Equal("SubscriptionName", Assert.IsType<ArgumentNullException>(exception.InnerException).ParamName);

        Assert.Equal(["A:queue"], deliveries.Values.ToArray());
    }

    [Theory(Timeout = 30000)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "saga-selection-preserves-independent-state")]
    public async Task SagaDispatch_PreservesSelectedTypesAndIndependentStateAsync(bool subscription, bool allSecond)
    {
        await using ServiceProvider provider = CreateProvider(new Deliveries(), includeSagas: true);
        using var receiver = new MessageReceiver(provider.GetRequiredService<IBusRegistrationContext>(),
            new PassiveBusHandle(), provider.GetRequiredService<IBusInstance>());
        ILoadSagaRepository<FirstSaga> first = provider.GetRequiredService<ILoadSagaRepository<FirstSaga>>();
        ILoadSagaRepository<SecondSaga> second = provider.GetRequiredService<ILoadSagaRepository<SecondSaga>>();
        Guid id = Guid.NewGuid();

        await DispatchAsync<FirstSaga>("first");
        Assert.Equal("first", (await first.LoadAsync(id, TestContext.Current.CancellationToken))!.Value);
        Assert.Null(await second.LoadAsync(id, TestContext.Current.CancellationToken));
        if (allSecond)
        {
            if (subscription)
                await receiver.HandleAsync("saga-topic", "shared", SagaMessage("second"), TestContext.Current.CancellationToken);
            else
                await receiver.HandleAsync("saga-queue", SagaMessage("second"), TestContext.Current.CancellationToken);
        }
        else
            await DispatchAsync<SecondSaga>("second");

        FirstSaga firstState = Assert.IsType<FirstSaga>(await first.LoadAsync(id, TestContext.Current.CancellationToken));
        SecondSaga secondState = Assert.IsType<SecondSaga>(await second.LoadAsync(id, TestContext.Current.CancellationToken));
        Assert.Equal(allSecond ? "second" : "first", firstState.Value);
        Assert.Equal(allSecond ? 2 : 1, firstState.Count);
        Assert.Equal("second", secondState.Value);
        Assert.Equal(1, secondState.Count);

        await DispatchAsync<FirstSaga>("third");
        Assert.Equal("third", firstState.Value);
        Assert.Equal(allSecond ? 3 : 2, firstState.Count);
        Assert.Equal("second", secondState.Value);
        Assert.Equal(1, secondState.Count);

        ServiceBusReceivedMessage SagaMessage(string value) => ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromObjectAsJson(new SagaPayload(id, value)), messageId: value,
            contentType: "application/json", deliveryCount: 1);

        Task DispatchAsync<TSaga>(string value) where TSaga : class, ISaga => subscription
            ? receiver.HandleSagaAsync<TSaga>("saga-topic", "shared", SagaMessage(value), TestContext.Current.CancellationToken)
            : receiver.HandleSagaAsync<TSaga>("saga-queue", SagaMessage(value), TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = 30000)]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "activity-types-and-consumer-role-are-distinct")]
    public async Task ActivityDispatch_SeparatesActivityTypesAndConsumerRoleAsync()
    {
        var deliveries = new Deliveries();
        await using ServiceProvider provider = CreateProvider(deliveries, includeActivities: true);
        using var receiver = new MessageReceiver(provider.GetRequiredService<IBusRegistrationContext>(),
            new PassiveBusHandle(), provider.GetRequiredService<IBusInstance>());

        await receiver.HandleConsumerAsync<FirstActivity>("shared-activities", Message("consumer"), TestContext.Current.CancellationToken);
        Assert.Equal(["consumer:consumer"], deliveries.Values.ToArray());
        await DispatchAsync<FirstActivity>("first");
        Assert.Equal(["consumer:consumer", "activity-A:first"], deliveries.Values.ToArray());
        await DispatchAsync<SecondActivity>("second");
        Assert.Equal(["consumer:consumer", "activity-A:first", "activity-B:second"], deliveries.Values.ToArray());
        await DispatchAsync<FirstActivity>("third");
        Assert.Equal(["consumer:consumer", "activity-A:first", "activity-B:second", "activity-A:third"], deliveries.Values.ToArray());

        Task DispatchAsync<TActivity>(string value) where TActivity : class
        {
            var builder = new RoutingSlipBuilder(Guid.NewGuid());
            builder.AddActivity("selected", new Uri("sb://127.0.0.1/shared-activities"), new Payload(value));
            builder.AddSubscription(new Uri("sb://127.0.0.1/unused-compensation-events"), RoutingSlipEvents.CompensationFailed);
            ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromBytes(ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(builder.Build()).ToArray()),
                messageId: value, contentType: "application/json", deliveryCount: 1);
            return receiver.HandleExecuteActivityAsync<TActivity>("shared-activities", message, TestContext.Current.CancellationToken);
        }
    }

    [Fact(Timeout = 30000)]
    [RequirementCoverage("REQ-VSB-ASB-FUNCTIONS-TESTING", "typed-bus-preserves-owner-and-consume-scope")]
    public async Task TypedBusDispatch_PreservesRegistrationOwnerAndAmbientContextAsync()
    {
        var deliveries = new Deliveries();
        var services = new ServiceCollection();
        services.AddSingleton(deliveries);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<FirstConsumer>();
            configuration.UsingAzureServiceBus((_, bus) => ConfigureBus(bus));
        });
        services.AddViciOneServiceBus<ISecondBus>(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<ScopedSecondConsumer, ScopedSecondConsumerDefinition>();
            configuration.UsingAzureServiceBus((_, bus) => ConfigureBus(bus));
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using var firstReceiver = new MessageReceiver(provider.GetRequiredService<IBusRegistrationContext>(),
            new PassiveBusHandle(), provider.GetRequiredService<Bind<IBus, IBusInstance>>().Value);
        using var secondReceiver = new MessageReceiver(provider.GetRequiredService<Bind<ISecondBus, IBusRegistrationContext>>().Value,
            new PassiveBusHandle(), provider.GetRequiredService<IBusInstance<ISecondBus>>());

        await firstReceiver.HandleAsync("same-path", Message("default-one"), TestContext.Current.CancellationToken);
        Assert.Equal(["A:default-one"], deliveries.Values.ToArray());
        await secondReceiver.HandleAsync("same-path", Message("typed-one"), TestContext.Current.CancellationToken);
        Assert.Equal(["A:default-one", "typed:typed-one"], deliveries.Values.ToArray());
        await firstReceiver.HandleAsync("second-path", Message("default-two"), TestContext.Current.CancellationToken);
        await secondReceiver.HandleAsync("second-path", Message("typed-two"), TestContext.Current.CancellationToken);
        Assert.Equal(["A:default-one", "typed:typed-one", "A:default-two", "typed:typed-two"], deliveries.Values.ToArray());

        static void ConfigureBus(IServiceBusBusFactoryConfigurator bus)
        {
            bus.Host("Endpoint=sb://127.0.0.1/;SharedAccessKeyName=local-test;SharedAccessKey=bG9jYWwtdGVzdA==");
            bus.UseRawJsonDeserializer(RawSerializerOptions.All, isDefault: true);
        }
    }

    private static ServiceProvider CreateProvider(Deliveries deliveries, bool includeSagas = false, bool includeActivities = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(deliveries);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<FirstConsumer>();
            configuration.AddConsumer<SecondConsumer>();
            if (includeSagas)
            {
                configuration.AddSaga<FirstSaga, LocalSagaDefinition<FirstSaga>>().InMemoryRepository();
                configuration.AddSaga<SecondSaga, LocalSagaDefinition<SecondSaga>>().InMemoryRepository();
            }
            if (includeActivities)
            {
                configuration.AddConsumer<FirstActivity>();
                configuration.AddExecuteActivity<FirstActivity, Payload>();
                configuration.AddExecuteActivity<SecondActivity, Payload>();
            }
            configuration.UsingAzureServiceBus((_, bus) =>
            {
                bus.Host("Endpoint=sb://127.0.0.1/;SharedAccessKeyName=local-test;SharedAccessKey=bG9jYWwtdGVzdA==");
                bus.UseRawJsonDeserializer(RawSerializerOptions.All, isDefault: true);
            });
        });
        return services.BuildServiceProvider(validateScopes: true);
    }

    public sealed record Payload(string Value);

    public interface ISecondBus : IBus;

    public sealed class ScopedSecondConsumer(ConsumeContext injected, Bind<ISecondBus, IScopedConsumeContextProvider> owner,
        Deliveries deliveries) : IConsumer<Payload>
    {
        public Task ConsumeAsync(ConsumeContext<Payload> context)
        {
            Assert.NotNull(context.MessageId);
            Assert.Equal(context.MessageId, injected.MessageId);
            Assert.True(injected.TryGetMessage<Payload>(out ConsumeContext<Payload>? injectedMessage));
            Assert.Equal(context.Message.Value, injectedMessage!.Message.Value);
            ConsumeContext owned = Assert.IsAssignableFrom<ConsumeContext>(owner.Value.GetContext());
            Assert.Equal(context.MessageId, owned.MessageId);
            Assert.True(owned.TryGetMessage<Payload>(out ConsumeContext<Payload>? ownedMessage));
            Assert.Equal(context.Message.Value, ownedMessage!.Message.Value);
            deliveries.Values.Enqueue("typed:" + context.Message.Value);
            return Task.CompletedTask;
        }
    }

    public sealed class ScopedSecondConsumerDefinition : ConsumerDefinition<ScopedSecondConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ScopedSecondConsumer> consumerConfigurator, IRegistrationContext context)
        {
            endpointConfigurator.PublishFaults = false;
        }
    }

    public sealed class FirstActivity(Deliveries deliveries) : IExecuteActivity<Payload>, IConsumer<Payload>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Payload> context)
        {
            deliveries.Values.Enqueue("activity-A:" + context.Arguments.Value);
            return Task.FromResult(context.Completed());
        }

        public Task ConsumeAsync(ConsumeContext<Payload> context)
        {
            deliveries.Values.Enqueue("consumer:" + context.Message.Value);
            return Task.CompletedTask;
        }
    }

    public sealed class SecondActivity(Deliveries deliveries) : IExecuteActivity<Payload>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Payload> context)
        {
            deliveries.Values.Enqueue("activity-B:" + context.Arguments.Value);
            return Task.FromResult(context.Completed());
        }
    }

    public sealed class LocalSagaDefinition<TSaga> : SagaDefinition<TSaga> where TSaga : class, ISaga
    {
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<TSaga> sagaConfigurator, IRegistrationContext context)
        {
            endpointConfigurator.PublishFaults = false;
        }
    }

    public sealed record SagaPayload(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    public sealed class FirstSaga : ISaga, IInitiatedByOrOrchestrates<SagaPayload>
    {
        public Guid CorrelationId { get; set; }
        public string? Value { get; private set; }
        public int Count { get; private set; }

        public Task ConsumeAsync(ConsumeContext<SagaPayload> context)
        {
            Value = context.Message.Value;
            Count++;
            return Task.CompletedTask;
        }
    }

    public sealed class SecondSaga : ISaga, IInitiatedByOrOrchestrates<SagaPayload>
    {
        public Guid CorrelationId { get; set; }
        public string? Value { get; private set; }
        public int Count { get; private set; }

        public Task ConsumeAsync(ConsumeContext<SagaPayload> context)
        {
            Value = context.Message.Value;
            Count++;
            return Task.CompletedTask;
        }
    }

    public sealed class Deliveries
    {
        public ConcurrentQueue<string> Values { get; } = new();
    }

    public sealed class FirstConsumer(Deliveries deliveries) : IConsumer<Payload>
    {
        public Task ConsumeAsync(ConsumeContext<Payload> context)
        {
            deliveries.Values.Enqueue("A:" + context.Message.Value);
            return Task.CompletedTask;
        }
    }

    public sealed class SecondConsumer(Deliveries deliveries) : IConsumer<Payload>
    {
        public Task ConsumeAsync(ConsumeContext<Payload> context)
        {
            deliveries.Values.Enqueue("B:" + context.Message.Value);
            return Task.CompletedTask;
        }
    }

    private sealed class PassiveBusHandle : IAsyncBusHandle
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
