using System.Buffers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Serialization;

public sealed class PayloadAdmissionMessagePackTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PAYLOAD-ADMISSION", "body-owned-capacity-boundaries-single-pass")]
    public async Task BodyOwnedCapacityLimit_EnforcesExactBoundariesWithoutReserializingAsync()
    {
        BoundaryPayload message = CreateMessage();
        int bodyLength = MeasureBodyLength(message);
        int requiredCapacity = await MeasureRequiredBodyCapacityAsync(bodyLength);

        Assert.True(requiredCapacity >= bodyLength);
        await AssertBodyBoundaryAsync(requiredCapacity + 1, rejected: false);
        await AssertBodyBoundaryAsync(requiredCapacity, rejected: false);
        await AssertBodyBoundaryAsync(requiredCapacity - 1, rejected: true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PAYLOAD-ADMISSION", "envelope-owned-capacity-boundaries-single-pass")]
    public async Task EnvelopeOwnedCapacityLimit_EnforcesExactBoundariesWithoutReserializingAsync()
    {
        int envelopeLength = await MeasureEnvelopeLengthAsync();
        int requiredCapacity = await MeasureRequiredEnvelopeCapacityAsync(envelopeLength);

        Assert.True(requiredCapacity >= envelopeLength);
        await AssertEnvelopeBoundaryAsync(requiredCapacity + 1, rejected: false);
        await AssertEnvelopeBoundaryAsync(requiredCapacity, rejected: false);
        await AssertEnvelopeBoundaryAsync(requiredCapacity - 1, rejected: true);
    }

    private static async Task AssertBodyBoundaryAsync(int maximumBodyBytes, bool rejected)
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
            Task send = SendAsync(bus, message);
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

    private static async Task<int> MeasureEnvelopeLengthAsync()
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
            await SendAsync(bus, message);
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

    private static async Task<int> MeasureRequiredBodyCapacityAsync(int bodyLength)
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
                () => SendAsync(bus, CreateMessage()));
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

    private static async Task<int> MeasureRequiredEnvelopeCapacityAsync(int envelopeLength)
    {
        BoundaryPayload.ResetSerializationReads();
        var observer = new BodyReadingObserver();
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = envelopeLength;
                options.MaximumTransportEnvelopeBytes = envelopeLength;
            },
            _ => throw new InvalidOperationException("A capacity probe must not reach the provider."));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => SendAsync(bus, CreateMessage()));
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

    private static async Task AssertEnvelopeBoundaryAsync(int maximumEnvelopeBytes, bool rejected)
    {
        BoundaryPayload.ResetSerializationReads();
        var observer = new BodyReadingObserver();
        var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = maximumEnvelopeBytes;
                options.MaximumTransportEnvelopeBytes = maximumEnvelopeBytes;
            },
            context => received.TrySetResult(context.Message.GetDataLength()));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            BoundaryPayload message = CreateMessage();
            Task send = SendAsync(bus, message);
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
        var admission = new PayloadAdmissionOptions<IBus>();
        configureAdmission(admission);
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(new MessageLimits
            {
                MaxBodyBytes = admission.MaximumSerializedBodyBytes!.Value,
                MaxEnvelopeBytes = admission.MaximumTransportEnvelopeBytes!.Value,
                MaxJsonDepth = 32,
                WarnAboveBytes = admission.WarningBodyBytes,
                OffloadToMessageDataAboveBytes = admission.MessageDataOffloadThresholdBytes,
            });
            configuration.UsingInMemory((_, bus) =>
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
            });
        });
        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static async Task SendAsync(IBus bus, BoundaryPayload message)
    {
        ISendEndpoint endpoint = await bus.GetSendEndpointAsync(
            new Uri("loopback://payload-messagepack/payload-messagepack-input"));
        await endpoint.SendAsync(message, TestContext.Current.CancellationToken);
    }

    private static BoundaryPayload CreateMessage() => new(new byte[12_000]);

    private static int MeasureBodyLength(BoundaryPayload message)
    {
        var writer = new ArrayBufferWriter<byte>();
        MessagePackSerializationRuntime.Serialize(writer, message);
        return writer.WrittenCount;
    }

    private sealed class BodyReadingObserver : ISendObserver
    {
        private int _preSendCalls;

        public int PreSendCalls => Volatile.Read(ref _preSendCalls);

        public long? BodyLength { get; private set; }

        public int ApplicationSerializationReads { get; private set; }

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _preSendCalls);
            TransportSendContext transport = Assert.IsType<TransportSendContext>(context, exactMatch: false);
            BodyLength = transport.Body.ToArray().LongLength;
            ApplicationSerializationReads = BoundaryPayload.SerializationReads;
            BoundaryPayload.StopCountingSerializationReads();
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
            => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
            => Task.CompletedTask;
    }
}
