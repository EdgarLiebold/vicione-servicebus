using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Events;

public sealed class PolymorphicFaultDispatchTests
{
    [Theory]
    [InlineData(PolymorphicShape.ExcludedBaseImplementsInterface)]
    [InlineData(PolymorphicShape.IncludedBaseImplementsInterface)]
    [InlineData(PolymorphicShape.DirectInterfaceWithoutBase)]
    [InlineData(PolymorphicShape.DirectInterfaceWithExcludedBase)]
    [InlineData(PolymorphicShape.DirectInterfaceWithIncludedBase)]
    [RequirementCoverage("REQ-VSB-POLYMORPHIC-FAULT-DISPATCH", "five-base-and-interface-shapes")]
    public Task EverySupportedPolymorphicShape_PublishesTypedAndInterfaceFaultsAsync(PolymorphicShape shape) =>
        shape switch
        {
            PolymorphicShape.ExcludedBaseImplementsInterface =>
                RunAsync<MessageWithoutDirectInterfaceAndExcludedBaseWithInterface>(),
            PolymorphicShape.IncludedBaseImplementsInterface =>
                RunAsync<MessageWithoutDirectInterfaceAndIncludedBaseWithInterface>(),
            PolymorphicShape.DirectInterfaceWithoutBase =>
                RunAsync<MessageWithDirectInterfaceAndNoBase>(),
            PolymorphicShape.DirectInterfaceWithExcludedBase =>
                RunAsync<MessageWithDirectInterfaceAndExcludedBase>(),
            PolymorphicShape.DirectInterfaceWithIncludedBase =>
                RunAsync<MessageWithDirectInterfaceAndIncludedBase>(),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "Unknown polymorphic shape."),
        };

    private static async Task RunAsync<T>()
        where T : class, IPolymorphicMessage, new()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<AlwaysFailConsumer<T>>();
                configuration.AddConsumer<FaultMessageConsumer<T>>();
                configuration.AddConsumer<FaultMessageConsumer<IPolymorphicMessage>>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync().WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            IConsumerTestHarness<FaultMessageConsumer<T>> typedConsumer =
                harness.GetConsumerHarness<FaultMessageConsumer<T>>();
            IConsumerTestHarness<FaultMessageConsumer<IPolymorphicMessage>> interfaceConsumer =
                harness.GetConsumerHarness<FaultMessageConsumer<IPolymorphicMessage>>();
            Task<IReceivedMessage<T>> failedConsumeTask = harness.Consumed
                .SelectAsync<T>(cancellationToken)
                .FirstObservedAsync();
            Task<IPublishedMessage<Fault<T>>> typedPublishTask = harness.Published
                .SelectAsync<Fault<T>>(cancellationToken)
                .FirstObservedAsync();
            Task<IPublishedMessage<Fault<IPolymorphicMessage>>> interfacePublishTask = harness.Published
                .SelectAsync<Fault<IPolymorphicMessage>>(cancellationToken)
                .FirstObservedAsync();
            var message = new T();

            await harness.Bus.PublishAsync(message, cancellationToken);

            IReceivedMessage<T> failedConsume = await failedConsumeTask.WaitAsync(timeout, cancellationToken);
            IPublishedMessage<Fault<T>> typedPublished = await typedPublishTask.WaitAsync(timeout, cancellationToken);
            IPublishedMessage<Fault<IPolymorphicMessage>> interfacePublished =
                await interfacePublishTask.WaitAsync(timeout, cancellationToken);
            IReceivedMessage<Fault<T>> typedConsumed = await typedConsumer.Consumed
                .SelectAsync<Fault<T>>(cancellationToken)
                .FirstObservedAsync()
                .WaitAsync(timeout, cancellationToken);
            IReceivedMessage<Fault<IPolymorphicMessage>> interfaceConsumed = await interfaceConsumer.Consumed
                .SelectAsync<Fault<IPolymorphicMessage>>(cancellationToken)
                .FirstObservedAsync()
                .WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;

            using var completed = new CancellationTokenSource();
            completed.Cancel();
            Assert.Single(harness.Published.Select<Fault<T>>(completed.Token));
            Assert.Single(harness.Published.Select<Fault<IPolymorphicMessage>>(completed.Token));
            Assert.IsType<PolymorphicFailureException>(failedConsume.Exception);
            Assert.Equal(message.CorrelationId, typedPublished.Context.Message.Message.CorrelationId);
            Assert.Equal(message.CorrelationId, interfacePublished.Context.Message.Message.CorrelationId);
            Assert.Equal(message.CorrelationId, typedConsumed.Context.Message.Message.CorrelationId);
            Assert.Equal(message.CorrelationId, interfaceConsumed.Context.Message.Message.CorrelationId);
            Assert.Contains(MessageUrn.ForTypeString<T>(), typedPublished.Context.Message.FaultMessageTypes);
            Assert.Contains(
                MessageUrn.ForTypeString<IPolymorphicMessage>(),
                typedPublished.Context.Message.FaultMessageTypes);
            Assert.Equal(
                TypeCache<PolymorphicFailureException>.ShortName,
                Assert.Single(typedPublished.Context.Message.Exceptions).ExceptionType);
        }
        finally
        {
            if (started)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public enum PolymorphicShape
    {
        ExcludedBaseImplementsInterface,
        IncludedBaseImplementsInterface,
        DirectInterfaceWithoutBase,
        DirectInterfaceWithExcludedBase,
        DirectInterfaceWithIncludedBase,
    }

    public interface IPolymorphicMessage
    {
        Guid CorrelationId { get; }
    }

    [ExcludeFromTopology]
    public abstract class ExcludedBaseWithInterface : IPolymorphicMessage
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    public abstract class IncludedBaseWithInterface : IPolymorphicMessage
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    [ExcludeFromTopology]
    public abstract class ExcludedBaseWithoutInterface;

    public abstract class IncludedBaseWithoutInterface;

    public sealed class MessageWithoutDirectInterfaceAndExcludedBaseWithInterface : ExcludedBaseWithInterface;

    public sealed class MessageWithoutDirectInterfaceAndIncludedBaseWithInterface : IncludedBaseWithInterface;

    public sealed class MessageWithDirectInterfaceAndNoBase : IPolymorphicMessage
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    public sealed class MessageWithDirectInterfaceAndExcludedBase : ExcludedBaseWithoutInterface, IPolymorphicMessage
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    public sealed class MessageWithDirectInterfaceAndIncludedBase : IncludedBaseWithoutInterface, IPolymorphicMessage
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
    }

    public sealed class AlwaysFailConsumer<T> : IConsumer<T>
        where T : class
    {
        public Task ConsumeAsync(ConsumeContext<T> context) =>
            Task.FromException(new PolymorphicFailureException());
    }

    public sealed class FaultMessageConsumer<T> : IConsumer<Fault<T>>
        where T : class
    {
        public Task ConsumeAsync(ConsumeContext<Fault<T>> context) => Task.CompletedTask;
    }

    private sealed class PolymorphicFailureException : Exception;
}
