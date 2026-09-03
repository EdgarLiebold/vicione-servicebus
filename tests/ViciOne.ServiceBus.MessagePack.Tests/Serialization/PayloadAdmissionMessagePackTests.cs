using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class PayloadAdmissionMessagePackTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PAYLOAD-ADMISSION", "body-owned-capacity-boundaries-single-pass")]
    public async Task BodyOwnedCapacityLimit_EnforcesExactBoundariesWithoutReserializing()
    {
        BoundaryPayload message = CreateMessage();
        int bodyLength = MeasureBodyLength(message);
        int requiredCapacity = await MeasureRequiredBodyCapacity(bodyLength);

        Assert.True(requiredCapacity >= bodyLength);
        await AssertBodyBoundary(requiredCapacity + 1, rejected: false);
        await AssertBodyBoundary(requiredCapacity, rejected: false);
        await AssertBodyBoundary(requiredCapacity - 1, rejected: true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PAYLOAD-ADMISSION", "envelope-owned-capacity-boundaries-single-pass")]
    public async Task EnvelopeOwnedCapacityLimit_EnforcesExactBoundariesWithoutReserializing()
    {
        int envelopeLength = await MeasureEnvelopeLength();
        int requiredCapacity = await MeasureRequiredEnvelopeCapacity(envelopeLength);

        Assert.True(requiredCapacity >= envelopeLength);
        await AssertEnvelopeBoundary(requiredCapacity + 1, rejected: false);
        await AssertEnvelopeBoundary(requiredCapacity, rejected: false);
        await AssertEnvelopeBoundary(requiredCapacity - 1, rejected: true);
    }

    private static async Task AssertBodyBoundary(int maximumBodyBytes, bool rejected)
    {
        BoundaryPayload message = CreateMessage();
        BoundaryPayload.ResetSerializationReads();
        var observer = new BodyReadingObserver();
        var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = maximumBodyBytes;
                options.MaximumTransportEnvelopeBytes = 1_000_000;
            },
            context => received.TrySetResult(context.Message.GetDataLength()));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            Task send = Send(bus, message);
            if (rejected)
            {
                PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(() => send);
                Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
                Assert.Equal(0, observer.PreSendCalls);
            }
            else
            {
                await send;
                Assert.Equal(message.GetDataLength(),
                    await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
                Assert.Equal(1, observer.PreSendCalls);
                Assert.Equal(1, observer.ApplicationSerializationReads);
            }

            if (rejected)
                Assert.Equal(1, BoundaryPayload.SerializationReads);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static async Task<int> MeasureEnvelopeLength()
    {
        BoundaryPayload.ResetSerializationReads();
        var observer = new BodyReadingObserver();
        var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = 1_000_000;
                options.MaximumTransportEnvelopeBytes = 1_000_000;
            },
            context => received.TrySetResult(context.Message.GetDataLength()));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            BoundaryPayload message = CreateMessage();
            await Send(bus, message);
            Assert.Equal(message.GetDataLength(),
                await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, observer.PreSendCalls);
            Assert.Equal(1, observer.ApplicationSerializationReads);
            Assert.True(observer.BodyLength.HasValue);
            return checked((int)observer.BodyLength.Value);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static async Task<int> MeasureRequiredBodyCapacity(int bodyLength)
    {
        BoundaryPayload.ResetSerializationReads();
        var observer = new BodyReadingObserver();
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = bodyLength;
                options.MaximumTransportEnvelopeBytes = 1_000_000;
            },
            _ => throw new InvalidOperationException("A capacity probe must not reach the provider."));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => Send(bus, CreateMessage()));
            Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
            Assert.Equal(1, BoundaryPayload.SerializationReads);
            Assert.Equal(0, observer.PreSendCalls);
            return checked((int)exception.ActualBytes);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static async Task<int> MeasureRequiredEnvelopeCapacity(int envelopeLength)
    {
        BoundaryPayload.ResetSerializationReads();
        var observer = new BodyReadingObserver();
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = 1_000_000;
                options.MaximumTransportEnvelopeBytes = envelopeLength;
            },
            _ => throw new InvalidOperationException("A capacity probe must not reach the provider."));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => Send(bus, CreateMessage()));
            Assert.Equal(PayloadAdmissionStage.TransportEnvelope, exception.Stage);
            Assert.Equal(1, BoundaryPayload.SerializationReads);
            Assert.Equal(0, observer.PreSendCalls);
            return checked((int)exception.ActualBytes);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static async Task AssertEnvelopeBoundary(int maximumEnvelopeBytes, bool rejected)
    {
        BoundaryPayload.ResetSerializationReads();
        var observer = new BodyReadingObserver();
        var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = 1_000_000;
                options.MaximumTransportEnvelopeBytes = maximumEnvelopeBytes;
            },
            context => received.TrySetResult(context.Message.GetDataLength()));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            BoundaryPayload message = CreateMessage();
            Task send = Send(bus, message);
            if (rejected)
            {
                PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(() => send);
                Assert.Equal(PayloadAdmissionStage.TransportEnvelope, exception.Stage);
                Assert.Equal(0, observer.PreSendCalls);
            }
            else
            {
                await send;
                Assert.Equal(message.GetDataLength(),
                    await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
                Assert.Equal(1, observer.PreSendCalls);
                Assert.Equal(1, observer.ApplicationSerializationReads);
                Assert.True(observer.BodyLength <= maximumEnvelopeBytes);
            }

            if (rejected)
                Assert.Equal(1, BoundaryPayload.SerializationReads);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static ServiceProvider BuildProvider(
        BodyReadingObserver observer,
        Action<PayloadAdmissionOptions<IBus>> configureAdmission,
        Action<ConsumeContext<BoundaryPayload>> consume)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOnePayloadAdmission(configureAdmission);
        services.AddViciOneServiceBus(configuration => configuration.UsingInMemory((_, bus) =>
        {
            bus.Host(new Uri("loopback://payload-messagepack/"));
            bus.ClearSerialization();
            bus.UseMessagePackSerializer();
            bus.ConnectSendObserver(observer);
            bus.ReceiveEndpoint("payload-messagepack-input", endpoint => endpoint.Handler<BoundaryPayload>(context =>
            {
                consume(context);
                return Task.CompletedTask;
            }));
        }));
        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static async Task Send(IBus bus, BoundaryPayload message)
    {
        ISendEndpoint endpoint = await bus.GetSendEndpoint(
            new Uri("loopback://payload-messagepack/payload-messagepack-input"));
        await endpoint.Send(message, TestContext.Current.CancellationToken);
    }

    private static BoundaryPayload CreateMessage() => new(new byte[12_000]);

    private static int MeasureBodyLength(BoundaryPayload message)
    {
        var writer = new ArrayBufferWriter<byte>();
        InternalMessagePackResolver.Serialize(typeof(BoundaryPayload), writer, message);
        return writer.WrittenCount;
    }

    private sealed class BodyReadingObserver : ISendObserver
    {
        private int _preSendCalls;

        public int PreSendCalls => Volatile.Read(ref _preSendCalls);

        public long? BodyLength { get; private set; }

        public int ApplicationSerializationReads { get; private set; }

        public Task PreSend<T>(SendContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _preSendCalls);
            TransportSendContext transport = Assert.IsAssignableFrom<TransportSendContext>(context);
            BodyLength = transport.Body.GetBytes().LongLength;
            ApplicationSerializationReads = BoundaryPayload.SerializationReads;
            BoundaryPayload.StopCountingSerializationReads();
            return Task.CompletedTask;
        }

        public Task PostSend<T>(SendContext<T> context)
            where T : class
            => Task.CompletedTask;

        public Task SendFault<T>(SendContext<T> context, Exception exception)
            where T : class
            => Task.CompletedTask;
    }
}
