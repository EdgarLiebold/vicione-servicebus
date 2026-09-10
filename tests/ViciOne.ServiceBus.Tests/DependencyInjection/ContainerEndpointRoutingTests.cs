using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ContainerEndpointRoutingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ENDPOINT-NAMING", "complete-registration-precedence-matrix")]
    public async Task ConsumerRegistrationPrecedence_ProducesEveryExactSourceAddressAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<PlainConsumer>();
                configuration.AddConsumer<DefinitionNameConsumer, DefinitionNameConsumerDefinition>();
                configuration.AddConsumer<InlineEndpointConsumer>()
                    .Endpoint(endpoint => endpoint.Name = "by-endpoint");
                configuration.AddConsumer<DefinitionEndpointConsumer, DefinitionEndpointConsumerDefinition>()
                    .Endpoint(endpoint => endpoint.Name = "by_endpoint_definition");
                configuration.AddConsumer<OverrideDefinitionNameConsumer, OverrideDefinitionNameConsumerDefinition>()
                    .Endpoint(endpoint => endpoint.Name = "by_endpoint_name");
                configuration.AddConsumer<OverrideDefinitionEndpointConsumer, OverrideDefinitionEndpointConsumerDefinition>()
                    .Endpoint(endpoint => endpoint.Name = "by_endpoint_override");
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            await harness.Bus.PublishAsync(new PlainCommand(NewId.NextGuid()), cancellationToken);
            await harness.Bus.PublishAsync(new DefinitionNameCommand(NewId.NextGuid()), cancellationToken);
            await harness.Bus.PublishAsync(new InlineEndpointCommand(NewId.NextGuid()), cancellationToken);
            await harness.Bus.PublishAsync(new DefinitionEndpointCommand(NewId.NextGuid()), cancellationToken);
            await harness.Bus.PublishAsync(new OverrideDefinitionNameCommand(NewId.NextGuid()), cancellationToken);
            await harness.Bus.PublishAsync(new OverrideDefinitionEndpointCommand(NewId.NextGuid()), cancellationToken);

            await AssertSourceAsync<PlainEvent, PlainConsumer>(harness, null, timeout, cancellationToken);
            await AssertSourceAsync<DefinitionNameEvent, DefinitionNameConsumer>(
                harness, "by_definition", timeout, cancellationToken);
            await AssertSourceAsync<InlineEndpointEvent, InlineEndpointConsumer>(
                harness, "by-endpoint", timeout, cancellationToken);
            await AssertSourceAsync<DefinitionEndpointEvent, DefinitionEndpointConsumer>(
                harness, "by_endpoint_definition", timeout, cancellationToken);
            await AssertSourceAsync<OverrideDefinitionNameEvent, OverrideDefinitionNameConsumer>(
                harness, "by_endpoint_name", timeout, cancellationToken);
            await AssertSourceAsync<OverrideDefinitionEndpointEvent, OverrideDefinitionEndpointConsumer>(
                harness, "by_endpoint_override", timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-ENDPOINT-NAMING", "custom-and-shared-endpoint-overrides")]
    public async Task InlineAndDefinitionEndpointNames_RouteAllAndOnlyTheirOwnedContractsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<CustomEndpointConsumer>()
                    .Endpoint(endpoint => endpoint.Name = "custom-endpoint-name");
                configuration.AddConsumer<SharedFirstConsumer, BrokenSharedDefinition>()
                    .Endpoint(endpoint => endpoint.Name = "shared-container-endpoint");
                configuration.AddConsumer<SharedSecondConsumer, SharedDefinition>();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            ISendEndpoint custom = await harness.Bus.GetSendEndpointAsync(new Uri("queue:custom-endpoint-name"), TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            ISendEndpoint shared = await harness.Bus.GetSendEndpointAsync(new Uri("queue:shared-container-endpoint"), TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            var customRequest = new CustomEndpointRequest(NewId.NextGuid());
            var firstRequest = new SharedFirstRequest(NewId.NextGuid());
            var secondRequest = new SharedSecondRequest(NewId.NextGuid());

            await custom.SendAsync(customRequest, cancellationToken);
            await shared.SendAsync(firstRequest, cancellationToken);
            await shared.SendAsync(secondRequest, cancellationToken);

            IPublishedMessage<CustomEndpointResult> customResult = await harness.Published
                .SelectAsync<CustomEndpointResult>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            IPublishedMessage<SharedFirstResult> firstResult = await harness.Published
                .SelectAsync<SharedFirstResult>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            IPublishedMessage<SharedSecondResult> secondResult = await harness.Published
                .SelectAsync<SharedSecondResult>(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            Assert.Equal(customRequest.CorrelationId, customResult.Context.Message.CorrelationId);
            Assert.Equal(firstRequest.CorrelationId, firstResult.Context.Message.CorrelationId);
            Assert.Equal(secondRequest.CorrelationId, secondResult.Context.Message.CorrelationId);
            Assert.Equal("custom-endpoint-name", customResult.Context.SourceAddress!.AbsolutePath.Trim('/'));
            Assert.Equal("shared-container-endpoint", firstResult.Context.SourceAddress!.AbsolutePath.Trim('/'));
            Assert.Equal("shared-container-endpoint", secondResult.Context.SourceAddress!.AbsolutePath.Trim('/'));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SERVICE-INSTANCE-ENDPOINTS", "kebab-consumer-and-nested-request-round-trip")]
    public async Task ServiceInstance_KeepsKebabEndpointAndCompletesTheNestedRequestAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var authorization = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(authorization)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.SetKebabCaseEndpointNameFormatter();
                configuration.AddConsumer<SubmitOrderConsumer>();
                configuration.AddConsumer<AuthorizeOrderConsumer>();
                configuration.AddRequestClient<SubmitOrder>();
                configuration.AddRequestClient<AuthorizeOrder>();
                configuration.UsingInMemory((context, bus) => bus.ConfigureServiceInstanceEndpoints(context));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid orderId = NewId.NextGuid();
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IRequestClient<SubmitOrder> client = scope.ServiceProvider.GetRequiredService<IRequestClient<SubmitOrder>>();
            Response<OrderAccepted> response = await client.Advanced().GetResponseAsync<OrderAccepted>(
                new SubmitOrder(orderId),
                timeout: new RequestTimeout(timeout),
                cancellationToken: cancellationToken);

            Assert.Equal(orderId, response.Message.OrderId);
            Assert.Equal(orderId, await authorization.Task.WaitAsync(timeout, cancellationToken));
            Assert.Equal(
                KebabCaseEndpointNameFormatter.Instance.Consumer<SubmitOrderConsumer>(),
                response.SourceAddress!.AbsolutePath.Trim('/'));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-RESOLUTION-FAULT", "missing-constructor-dependency-is-a-published-fault")]
    public async Task MissingConsumerDependency_IsCapturedAsOneEndpointFaultAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<MissingDependencyConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            var command = new MissingDependencyCommand(NewId.NextGuid());
            await harness.Bus.PublishAsync(command, cancellationToken);
            IPublishedMessage<Fault<MissingDependencyCommand>> fault = await harness.Published
                .SelectAsync<Fault<MissingDependencyCommand>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Assert.Equal(command.CorrelationId, fault.Context.Message.Message.CorrelationId);
            Assert.Contains(fault.Context.Message.Exceptions,
                exception => exception.Message.Contains(nameof(IMissingDependency), StringComparison.Ordinal));
            Assert.Single(harness.Published.Snapshot<Fault<MissingDependencyCommand>>());
        }
        finally
        {
            if (started)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static async Task AssertSourceAsync<TEvent, TConsumer>(
        ITestHarness harness,
        string? endpointName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        where TEvent : class
        where TConsumer : class, IConsumer
    {
        IPublishedMessage<TEvent> published = await harness.Published.SelectAsync<TEvent>(cancellationToken)
            .FirstObservedAsync(cancellationToken: cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        string expected = endpointName ?? DefaultEndpointNameFormatter.Instance.Consumer<TConsumer>();
        Assert.Equal(expected, published.Context.SourceAddress!.AbsolutePath.Trim('/'));
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;


    public sealed record PlainCommand(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record PlainEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record DefinitionNameCommand(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record DefinitionNameEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record InlineEndpointCommand(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record InlineEndpointEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record DefinitionEndpointCommand(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record DefinitionEndpointEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record OverrideDefinitionNameCommand(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record OverrideDefinitionNameEvent(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record OverrideDefinitionEndpointCommand(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record OverrideDefinitionEndpointEvent(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class PlainConsumer : PublishingConsumer<PlainCommand, PlainEvent>;
    public sealed class DefinitionNameConsumer : PublishingConsumer<DefinitionNameCommand, DefinitionNameEvent>;
    public sealed class InlineEndpointConsumer : PublishingConsumer<InlineEndpointCommand, InlineEndpointEvent>;
    public sealed class DefinitionEndpointConsumer : PublishingConsumer<DefinitionEndpointCommand, DefinitionEndpointEvent>;
    public sealed class OverrideDefinitionNameConsumer :
        PublishingConsumer<OverrideDefinitionNameCommand, OverrideDefinitionNameEvent>;
    public sealed class OverrideDefinitionEndpointConsumer :
        PublishingConsumer<OverrideDefinitionEndpointCommand, OverrideDefinitionEndpointEvent>;

    public abstract class PublishingConsumer<TCommand, TEvent> : IConsumer<TCommand>
        where TCommand : class, CorrelatedBy<Guid>
        where TEvent : class, CorrelatedBy<Guid>
    {
        public Task ConsumeAsync(ConsumeContext<TCommand> context) => context.Advanced().PublishAsync(
            Activator.CreateInstance(typeof(TEvent), context.Message.CorrelationId)!,
            context.CancellationToken);
    }

    public sealed class DefinitionNameConsumerDefinition : ConsumerDefinition<DefinitionNameConsumer>
    {
        public DefinitionNameConsumerDefinition() => EndpointName = "by_definition";
    }

    public sealed class DefinitionEndpointConsumerDefinition : ConsumerDefinition<DefinitionEndpointConsumer>
    {
        public DefinitionEndpointConsumerDefinition() => Endpoint(endpoint => endpoint.Name = "ignored-definition-endpoint");
    }

    public sealed class OverrideDefinitionNameConsumerDefinition : ConsumerDefinition<OverrideDefinitionNameConsumer>
    {
        public OverrideDefinitionNameConsumerDefinition() => EndpointName = "by_endpoint_name";
    }

    public sealed class OverrideDefinitionEndpointConsumerDefinition : ConsumerDefinition<OverrideDefinitionEndpointConsumer>
    {
        public OverrideDefinitionEndpointConsumerDefinition() =>
            Endpoint(endpoint => endpoint.Name = "ignored_definition_endpoint");
    }

    public sealed record CustomEndpointRequest(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record CustomEndpointResult(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record SharedFirstRequest(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record SharedFirstResult(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record SharedSecondRequest(Guid CorrelationId) : CorrelatedBy<Guid>;
    public sealed record SharedSecondResult(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class CustomEndpointConsumer : PublishingConsumer<CustomEndpointRequest, CustomEndpointResult>;
    public sealed class SharedFirstConsumer : PublishingConsumer<SharedFirstRequest, SharedFirstResult>;
    public sealed class SharedSecondConsumer : PublishingConsumer<SharedSecondRequest, SharedSecondResult>;

    public sealed class BrokenSharedDefinition : ConsumerDefinition<SharedFirstConsumer>
    {
        public BrokenSharedDefinition() => Endpoint(endpoint => endpoint.Name = "broken-shared-endpoint");
    }

    public sealed class SharedDefinition : ConsumerDefinition<SharedSecondConsumer>
    {
        public SharedDefinition() => Endpoint(endpoint => endpoint.Name = "shared-container-endpoint");
    }

    public sealed record SubmitOrder(Guid OrderId) : CorrelatedBy<Guid>
    {
        public Guid CorrelationId => OrderId;
    }

    public sealed record AuthorizeOrder(Guid OrderId) : CorrelatedBy<Guid>
    {
        public Guid CorrelationId => OrderId;
    }

    public sealed record OrderAuthorized(Guid OrderId);
    public sealed record OrderAccepted(Guid OrderId);

    public sealed class SubmitOrderConsumer(IRequestClient<AuthorizeOrder> authorizeClient) : IConsumer<SubmitOrder>
    {
        public async Task ConsumeAsync(ConsumeContext<SubmitOrder> context)
        {
            Response<OrderAuthorized> authorized = await authorizeClient.GetResponseAsync<OrderAuthorized>(
                new AuthorizeOrder(context.Message.OrderId),
                context.CancellationToken);
            await context.RespondAsync(new OrderAccepted(authorized.Message.OrderId));
        }
    }

    public sealed class AuthorizeOrderConsumer(TaskCompletionSource<Guid> authorization) : IConsumer<AuthorizeOrder>
    {
        public Task ConsumeAsync(ConsumeContext<AuthorizeOrder> context)
        {
            authorization.TrySetResult(context.Message.OrderId);
            return context.RespondAsync(new OrderAuthorized(context.Message.OrderId));
        }
    }

    public sealed record MissingDependencyCommand(Guid CorrelationId) : CorrelatedBy<Guid>;
    public interface IMissingDependency;

    public sealed class MissingDependencyConsumer(IMissingDependency dependency) : IConsumer<MissingDependencyCommand>
    {
        public Task ConsumeAsync(ConsumeContext<MissingDependencyCommand> context)
        {
            GC.KeepAlive(dependency);
            return Task.CompletedTask;
        }
    }
}
