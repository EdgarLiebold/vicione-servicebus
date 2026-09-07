using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

public sealed class AmazonSqsMultiBusTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0226", "two-buses-keep-independent-entity-name-formatters")]
    public async Task TwoBuses_KeepIndependentEntityNameFormattersAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("multibus");
        string firstQueueName = fixture.Name("first");
        string secondQueueName = fixture.Name("second");
        var firstDeliveries = new DeliveryRecorder();
        var secondDeliveries = new DeliveryRecorder();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddOptions<ViciOneServiceBusHostOptions>().Configure(options =>
        {
            options.WaitUntilStarted = true;
            options.StartTimeout = fixture.OperationTimeout;
            options.StopTimeout = fixture.OperationTimeout;
        });
        services
            .AddViciOneServiceBus(registration =>
            {
                registration.Limits(MessageLimits.Conservative);
                registration.UsingAmazonSqs((_, configurator) =>
                {
                    configurator.MessageTopology.SetEntityNameFormatter(new BusEntityNameFormatter("server1"));
                    fixture.ConfigureHost(configurator);
                    configurator.ReceiveEndpoint(firstQueueName, endpoint =>
                    {
                        endpoint.Handler<EndpointProbe>(firstDeliveries.ObserveProbeAsync);
                        endpoint.Handler<MultiBusMessage>(firstDeliveries.ObserveAsync);
                    });
                });
            })
            .AddViciOneServiceBus<ISecondBus>(registration =>
            {
                registration.Limits(MessageLimits.Conservative);
                registration.UsingAmazonSqs((_, configurator) =>
                {
                    configurator.MessageTopology.SetEntityNameFormatter(new BusEntityNameFormatter("server2"));
                    fixture.ConfigureHost(configurator);
                    configurator.ReceiveEndpoint(secondQueueName, endpoint =>
                    {
                        endpoint.Handler<EndpointProbe>(secondDeliveries.ObserveProbeAsync);
                        endpoint.Handler<MultiBusMessage>(secondDeliveries.ObserveAsync);
                    });
                });
            });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        IBus firstBus = provider.GetRequiredService<IBus>();
        ISecondBus secondBus = provider.GetRequiredService<ISecondBus>();
        IHostedService[] hostedServices =
            [.. provider.GetServices<IHostedService>().Where(service => service.GetType().FullName == "ViciOne.ServiceBus.ServiceBusHostedService")];
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        int startedServiceCount = 0;

        try
        {
            Assert.NotSame(firstBus, secondBus);
            Assert.Single(hostedServices);

            foreach (IHostedService hostedService in hostedServices)
            {
                await hostedService.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
                startedServiceCount++;
            }

            Assert.True(firstBus.Topology.TryGetPublishAddress<MultiBusMessage>(out Uri? firstAddress));
            Assert.True(secondBus.Topology.TryGetPublishAddress<MultiBusMessage>(out Uri? secondAddress));
            Assert.Equal(
                new Uri($"amazonsqs://{fixture.Region}/{fixture.Prefix}_server1-topic-{nameof(MultiBusMessage)}?type=topic"),
                firstAddress);
            Assert.Equal(
                new Uri($"amazonsqs://{fixture.Region}/{fixture.Prefix}_server2-topic-{nameof(MultiBusMessage)}?type=topic"),
                secondAddress);
            Assert.NotEqual(firstAddress, secondAddress);

            using var firstPublishObserver = new PublishDestinationObserver();
            using var secondPublishObserver = new PublishDestinationObserver();
            using ConnectHandle firstObserverHandle = firstBus.ConnectPublishObserver(firstPublishObserver);
            using ConnectHandle secondObserverHandle = secondBus.ConnectPublishObserver(secondPublishObserver);
            ISendEndpoint firstEndpoint = await firstBus.GetSendEndpointAsync(new Uri($"queue:{firstQueueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            ISendEndpoint secondEndpoint = await secondBus.GetSendEndpointAsync(new Uri($"queue:{secondQueueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            Guid firstProbeId = Guid.NewGuid();
            Guid secondProbeId = Guid.NewGuid();
            await firstEndpoint.SendAsync(new EndpointProbe(firstProbeId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await secondEndpoint.SendAsync(new EndpointProbe(secondProbeId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(firstProbeId, await firstDeliveries.Probe.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(secondProbeId, await secondDeliveries.Probe.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            Guid firstId = Guid.NewGuid();
            Guid secondId = Guid.NewGuid();
            await firstBus.PublishAsync(new MultiBusMessage(firstId, 1), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await secondBus.PublishAsync(new MultiBusMessage(secondId, 2), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal([firstAddress], firstPublishObserver.Destinations);
            Assert.Equal([secondAddress], secondPublishObserver.Destinations);

            Assert.Equal(firstId, await firstDeliveries.First.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(secondId, await secondDeliveries.First.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            for (int index = startedServiceCount - 1; index >= 0; index--)
                await hostedServices[index].StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            startedServiceCount = 0;

            Assert.Equal([new MultiBusMessage(firstId, 1)], firstDeliveries.Deliveries);
            Assert.Equal([new MultiBusMessage(secondId, 2)], secondDeliveries.Deliveries);
        }
        finally
        {
            for (int index = startedServiceCount - 1; index >= 0; index--)
                await hostedServices[index].StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    public interface ISecondBus : IBus
    {
    }

    public sealed record MultiBusMessage(Guid CorrelationId, int Server);

    private sealed record EndpointProbe(Guid CorrelationId);

    private sealed class BusEntityNameFormatter(string server) : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => $"{server}-topic-{typeof(T).Name}";
    }

    private sealed class DeliveryRecorder
    {
        public ConcurrentQueue<MultiBusMessage> Deliveries { get; } = [];
        public TaskCompletionSource<Guid> First { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Guid> Probe { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ObserveAsync(ConsumeContext<MultiBusMessage> context)
        {
            Deliveries.Enqueue(context.Message);
            First.TrySetResult(context.Message.CorrelationId);
            return Task.CompletedTask;
        }

        public Task ObserveProbeAsync(ConsumeContext<EndpointProbe> context)
        {
            Probe.TrySetResult(context.Message.CorrelationId);
            return Task.CompletedTask;
        }
    }

    private sealed class PublishDestinationObserver : IPublishObserver, IDisposable
    {
        public ConcurrentQueue<Uri?> Destinations { get; } = [];

        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            if (context.Message is MultiBusMessage)
                Destinations.Enqueue(context.DestinationAddress);
            return Task.CompletedTask;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;

        public void Dispose()
        {
        }
    }
}
