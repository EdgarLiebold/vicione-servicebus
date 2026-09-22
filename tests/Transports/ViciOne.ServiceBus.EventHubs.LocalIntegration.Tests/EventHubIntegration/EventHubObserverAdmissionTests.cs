using System.Collections.Concurrent;
using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubObserverAdmissionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PAYLOAD-ADMISSION", "single-observer-content-type-mutation-is-rejected")]
    public async Task SingleProduce_RejectsObserverContentTypeMutationAsync()
    {
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("single-admission");
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider
                .GetRequiredService<IEventHubProducerProvider>()
                .GetProducerAsync("envelope-eh", cancellationToken: cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            var observer = new ContentTypeChangingObserver(targetIndex: 1);
            using ConnectHandle handle = producer.ConnectSendObserver(observer);

            ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() =>
                producer.ProduceAsync(new AdmissionMessage(1, "reject"), cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken));

            Assert.Contains("content type no longer matches its serializer", failure.Message, StringComparison.Ordinal);
            Assert.Equal(observer.SerializerContentType, observer.OriginalContentType);
            Assert.NotEqual(observer.OriginalContentType, observer.MutatedContentType);
            Assert.Equal([1], observer.PreSendIndices);
            Assert.Empty(observer.PostSendIndices);
            Assert.Same(failure, Assert.Single(observer.Faults));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-EVENTHUB-PAYLOAD-ADMISSION", "each-batch-observer-content-type-mutation-is-rejected")]
    public async Task BatchProduce_RejectsEitherObserverContentTypeMutationAsync(int targetIndex)
    {
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create($"batch-admission-{targetIndex}");
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider
                .GetRequiredService<IEventHubProducerProvider>()
                .GetProducerAsync("envelope-eh", cancellationToken: cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            var observer = new ContentTypeChangingObserver(targetIndex);
            using ConnectHandle handle = producer.ConnectSendObserver(observer);

            ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() =>
                producer.ProduceAsync<AdmissionMessage>(new[]
                {
                    new AdmissionMessage(1, "valid-first"),
                    new AdmissionMessage(2, "mutated-second"),
                }, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken));

            Assert.Contains("content type no longer matches its serializer", failure.Message, StringComparison.Ordinal);
            Assert.Equal(observer.SerializerContentType, observer.OriginalContentType);
            Assert.NotEqual(observer.OriginalContentType, observer.MutatedContentType);
            Assert.Equal([1, 2], observer.PreSendIndices.Order());
            Assert.Empty(observer.PostSendIndices);
            Assert.Equal(2, observer.Faults.Length);
            Assert.All(observer.Faults, fault => Assert.Same(failure, fault));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    static ServiceProvider CreateProvider(EventHubLocalFixture fixture) => new ServiceCollection()
        .AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory();
            configuration.AddRider(rider => rider.UsingEventHub((_, eventHubs) => fixture.Configure(eventHubs)));
        })
        .BuildServiceProvider(true);

    sealed record AdmissionMessage(int Index, string Value);

    sealed class ContentTypeChangingObserver(int targetIndex) : ISendObserver
    {
        readonly ConcurrentQueue<int> _preSendIndices = new();
        readonly ConcurrentQueue<int> _postSendIndices = new();
        readonly ConcurrentQueue<Exception> _faults = new();

        public int[] PreSendIndices => _preSendIndices.ToArray();
        public int[] PostSendIndices => _postSendIndices.ToArray();
        public Exception[] Faults => _faults.ToArray();
        public string? OriginalContentType { get; private set; }
        public string? SerializerContentType { get; private set; }
        public string? MutatedContentType { get; private set; }

        public async Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            if (context.Message is not AdmissionMessage message)
                throw new InvalidOperationException("The admission observer received an unrelated message.");

            _preSendIndices.Enqueue(message.Index);
            await Task.Yield();
            if (message.Index == targetIndex)
            {
                OriginalContentType = context.ContentType?.ToString();
                SerializerContentType = context.Serializer.ContentType.ToString();
                context.ContentType = new ContentType("application/octet-stream");
                MutatedContentType = context.ContentType.ToString();
            }
        }

        public Task PostSendAsync<T>(SendContext<T> context) where T : class
        {
            if (context.Message is not AdmissionMessage message)
                throw new InvalidOperationException("The admission observer received an unrelated message.");

            _postSendIndices.Enqueue(message.Index);
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class
        {
            _faults.Enqueue(exception);
            return Task.CompletedTask;
        }
    }
}
