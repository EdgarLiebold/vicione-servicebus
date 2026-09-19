using System.Buffers;
using MessagePack;
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

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PAYLOAD-ADMISSION", "copied-envelope-exact-body-and-wire-snapshot")]
    public async Task CopiedEnvelope_AdmitsItsEmbeddedBodyAndPreservesTheWireBytesAsync()
    {
        var source = new BoundaryPayload(Enumerable.Repeat((byte)0x5A, 128).ToArray());
        byte[] serializedBody = MessagePackSerializationRuntime.Serialize(source);
        byte[] envelope = new MessagePackMessageSerializer()
            .GetMessageBody(new MessageSendContext<BoundaryPayload>(source))
            .ToArray();
        Assert.True(envelope.Length > serializedBody.Length);

        BoundaryPayload.ResetSerializationReads();
        var observer = new BodyReadingObserver();
        var received = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = serializedBody.Length;
                options.MaximumTransportEnvelopeBytes = envelope.Length;
            },
            context => received.TrySetResult(context.Message.Data.ToArray()));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            await SendCopiedAsync(bus, envelope);

            Assert.Equal(source.Data, await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Equal(envelope, observer.EnvelopeBytes);
            Assert.Equal(envelope.Length, observer.BodyLength);
            Assert.Equal(1, observer.PreSendCalls);
            Assert.Equal(0, observer.ApplicationSerializationReads);
        }
        finally
        {
            BoundaryPayload.StopCountingSerializationReads();
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PAYLOAD-ADMISSION", "copied-envelope-embedded-body-one-byte-over-limit")]
    public async Task CopiedEnvelope_RejectsTheFullEmbeddedBodyOneByteOverLimitAsync()
    {
        var source = new BoundaryPayload(Enumerable.Repeat((byte)0x5A, 128).ToArray());
        byte[] serializedBody = MessagePackSerializationRuntime.Serialize(source);
        byte[] envelope = new MessagePackMessageSerializer()
            .GetMessageBody(new MessageSendContext<BoundaryPayload>(source))
            .ToArray();
        int bodyLimit = serializedBody.Length - 1;

        BoundaryPayload.ResetSerializationReads();
        var observer = new BodyReadingObserver();
        var consumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = bodyLimit;
                options.MaximumTransportEnvelopeBytes = envelope.Length;
            },
            _ => consumed.TrySetResult());
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => SendCopiedAsync(bus, envelope));
            Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
            Assert.Equal(serializedBody.LongLength, exception.ActualBytes);
            Assert.Equal(bodyLimit, exception.ConfiguredLimitBytes);
            Assert.Equal(0, observer.PreSendCalls);
            Assert.False(consumed.Task.IsCompleted);
            Assert.Equal(0, BoundaryPayload.SerializationReads);
        }
        finally
        {
            BoundaryPayload.StopCountingSerializationReads();
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PAYLOAD-ADMISSION", "copied-envelope-ambiguous-or-trailing-content-rejected")]
    public async Task CopiedEnvelope_RejectsAmbiguousOrTrailingContentBeforeTransportAsync()
    {
        byte[] serializedBody = MessagePackSerializationRuntime.Serialize(new BoundaryPayload([0x5A]));
        byte[] validEnvelope = EnvelopeWithMessage(serializedBody);
        (byte[] Envelope, string ExpectedReason)[] malformed =
        [
            ([0xC0], "not a map"),
            (EnvelopeWithMessage(serializedBody, numericKey: true), "non-string key"),
            (EnvelopeWithMessage(serializedBody, duplicate: true), "more than one message value"),
            (EnvelopeWithMessage(serializedBody, key: "Other"), "no unique message value"),
            (EnvelopeWithMessage(serializedBody, nilValue: true), "message is not binary"),
            (EnvelopeWithMessage([]), "no serialized message"),
            (EnvelopeWithMessage([.. serializedBody, 0xC0]), "message has trailing data"),
            ([.. validEnvelope, 0xC0], "has trailing data"),
            (validEnvelope[..^1], "malformed"),
        ];
        var observer = new BodyReadingObserver();
        var consumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildProvider(
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = 1_000;
                options.MaximumTransportEnvelopeBytes = 1_000;
            },
            _ => consumed.TrySetResult());
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            foreach ((byte[] envelope, string expectedReason) in malformed)
            {
                InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => SendCopiedAsync(bus, envelope));
                Assert.Contains(expectedReason, exception.Message, StringComparison.Ordinal);
            }

            Assert.Equal(0, observer.PreSendCalls);
            Assert.False(consumed.Task.IsCompleted);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static byte[] EnvelopeWithMessage(
        byte[] serializedBody,
        string key = "Message",
        bool duplicate = false,
        bool nilValue = false,
        bool numericKey = false)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteMapHeader(duplicate ? 2 : 1);
        if (numericKey)
            writer.Write(1);
        else
            writer.Write(key);

        if (nilValue)
            writer.WriteNil();
        else
            writer.Write(serializedBody.AsSpan());
        if (duplicate)
        {
            writer.Write("Message");
            writer.Write(serializedBody.AsSpan());
        }

        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    private static async Task SendCopiedAsync(IBus bus, byte[] envelope)
    {
        ISendEndpoint endpoint = await bus.GetSendEndpointAsync(
            new Uri("loopback://payload-messagepack/payload-messagepack-input"));
        await endpoint.SendAsync(
            new BoundaryPayload([0x7F]),
            context => context.Serializer = new CopyBodySerializer(
                MessagePackMessageSerializer.MessagePackContentType,
                new BinaryMessageBody(envelope)),
            TestContext.Current.CancellationToken);
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

        public byte[]? EnvelopeBytes { get; private set; }

        public int ApplicationSerializationReads { get; private set; }

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _preSendCalls);
            TransportSendContext transport = Assert.IsType<TransportSendContext>(context, exactMatch: false);
            EnvelopeBytes = transport.Body.ToArray();
            BodyLength = EnvelopeBytes.LongLength;
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
