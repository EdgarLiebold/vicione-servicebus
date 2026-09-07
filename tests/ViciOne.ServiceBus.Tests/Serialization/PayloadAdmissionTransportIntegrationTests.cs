using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Serialization;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class PayloadAdmissionTransportIntegrationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "json-body-rejection-before-observers-and-provider")]
    public async Task JsonBodyLimit_RejectsBeforeUserObserversAndProviderDeliveryAsync()
    {
        var converter = new CountingPayloadConverter();
        var observer = new BodyReadingObserver();
        var delivered = 0;
        await using ServiceProvider provider = BuildJsonProvider(
            converter,
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = 1;
                options.MaximumTransportEnvelopeBytes = 1_000_000;
            },
            _ => Interlocked.Increment(ref delivered));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => SendJsonAsync(bus, new CountingPayload("body-rejection")));

            Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
            Assert.Equal(1, converter.WriteCalls);
            Assert.Equal(0, observer.PreSendCalls);
            Assert.Equal(0, Volatile.Read(ref delivered));
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "json-envelope-rejection-single-body-pass")]
    public async Task JsonEnvelopeLimit_RejectsAfterOneBodySerializationAndBeforeUserObserversAsync()
    {
        var converter = new CountingPayloadConverter();
        var observer = new BodyReadingObserver();
        var delivered = 0;
        await using ServiceProvider provider = BuildJsonProvider(
            converter,
            observer,
            options =>
            {
                // Utf8JsonWriter conservatively reserves a 256-byte initial span even though
                // this application's encoded body is smaller. Keep that bounded reservation
                // admissible so this test reaches the independently bounded JSON envelope.
                options.MaximumSerializedBodyBytes = 256;
                options.MaximumTransportEnvelopeBytes = 256;
            },
            _ => Interlocked.Increment(ref delivered));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => SendJsonAsync(bus, new CountingPayload("envelope-rejection")));

            Assert.Equal(PayloadAdmissionStage.TransportEnvelope, exception.Stage);
            Assert.Equal(1, converter.WriteCalls);
            Assert.Equal(0, observer.PreSendCalls);
            Assert.Equal(0, Volatile.Read(ref delivered));
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "json-success-single-pass-and-cache")]
    public async Task JsonAdmission_SerializesTheApplicationOnceAndSharesTheAdmittedBodyAsync()
    {
        var converter = new CountingPayloadConverter();
        var observer = new BodyReadingObserver();
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildJsonProvider(
            converter,
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = 1_000_000;
                options.MaximumTransportEnvelopeBytes = 1_000_000;
            },
            context => received.TrySetResult(context.Message.Value));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            await SendJsonAsync(bus, new CountingPayload("accepted"));

            Assert.Equal("accepted", await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, converter.WriteCalls);
            Assert.Equal(1, observer.PreSendCalls);
            Assert.True(observer.FirstBodyLength > 0);
            Assert.Equal(observer.FirstBodyLength, observer.SecondBodyLength);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "json-body-exact-boundaries")]
    public async Task JsonBodyLimit_EnforcesExactBoundariesOnTheRealSerializerAsync(
        int bytesOverLimit,
        bool rejected)
    {
        var message = new BoundaryPayload(BoundaryPayload.ItemCount);
        var converter = new CountingBoundaryPayloadConverter();
        var observer = new BodyReadingObserver();
        var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildJsonBoundaryProvider(
            converter,
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = BoundaryPayload.SerializedLength - bytesOverLimit;
                options.MaximumTransportEnvelopeBytes = 1_000_000;
            },
            context => received.TrySetResult(context.Message.ValueCount));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            Task send = SendJsonAsync(bus, message);
            if (rejected)
            {
                PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(() => send);
                Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
                Assert.Equal(0, observer.PreSendCalls);
            }
            else
            {
                await send;
                Assert.Equal(BoundaryPayload.ItemCount,
                    await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
                Assert.Equal(1, observer.PreSendCalls);
            }

            Assert.Equal(1, converter.WriteCalls);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "json-envelope-owned-capacity-boundaries")]
    public void JsonEnvelopeOwnedCapacityLimit_EnforcesExactBoundariesOnTheRealSerializer()
    {
        JsonEnvelopeSerializationAttempt baseline = SerializeDeterministicJsonEnvelope(1_000_000);
        Assert.Null(baseline.Rejection);
        Assert.NotNull(baseline.Bytes);
        Assert.Equal(1, baseline.ConverterWriteCalls);

        int envelopeLength = baseline.Bytes.Length;
        int requiredCapacity = FindMinimumJsonEnvelopeCapacity(envelopeLength);

        Assert.True(envelopeLength > BoundaryPayload.SerializedLength);
        Assert.True(requiredCapacity >= envelopeLength);
        AssertJsonEnvelopeBoundary(requiredCapacity + 1, rejected: false);
        AssertJsonEnvelopeBoundary(requiredCapacity, rejected: false);
        AssertJsonEnvelopeBoundary(requiredCapacity - 1, rejected: true);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "raw-json-body-exact-boundaries")]
    public async Task RawJsonBodyLimit_EnforcesExactBoundariesWithoutReserializingAsync(
        int bytesOverLimit,
        bool rejected)
    {
        var message = new BoundaryPayload(BoundaryPayload.ItemCount);
        var converter = new CountingBoundaryPayloadConverter();
        var observer = new BodyReadingObserver();
        var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildRawJsonBoundaryProvider(
            converter,
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = BoundaryPayload.SerializedLength - bytesOverLimit;
                options.MaximumTransportEnvelopeBytes = 1_000_000;
            },
            context => received.TrySetResult(context.Message.ValueCount));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            Task send = SendRawJsonAsync(bus, message);
            if (rejected)
            {
                PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(() => send);
                Assert.Equal(PayloadAdmissionStage.SerializedBody, exception.Stage);
                Assert.Equal(0, observer.PreSendCalls);
            }
            else
            {
                await send;
                Assert.Equal(BoundaryPayload.ItemCount,
                    await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
                Assert.Equal(1, observer.PreSendCalls);
                Assert.Equal(BoundaryPayload.SerializedLength, observer.FirstBodyLength);
            }

            Assert.Equal(1, converter.WriteCalls);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "raw-json-envelope-exact-boundaries")]
    public async Task RawJsonEnvelopeLimit_EnforcesExactBoundariesWithoutReserializingAsync(
        int bytesOverLimit,
        bool rejected)
    {
        var message = new BoundaryPayload(BoundaryPayload.ItemCount);
        var converter = new CountingBoundaryPayloadConverter();
        var observer = new BodyReadingObserver();
        var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildRawJsonBoundaryProvider(
            converter,
            observer,
            options =>
            {
                options.MaximumSerializedBodyBytes = BoundaryPayload.SerializedLength;
                options.MaximumTransportEnvelopeBytes = BoundaryPayload.SerializedLength - bytesOverLimit;
            },
            context => received.TrySetResult(context.Message.ValueCount));
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            Task send = SendRawJsonAsync(bus, message);
            if (rejected)
            {
                PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(() => send);
                Assert.Equal(PayloadAdmissionStage.TransportEnvelope, exception.Stage);
                Assert.Equal(0, observer.PreSendCalls);
            }
            else
            {
                await send;
                Assert.Equal(BoundaryPayload.ItemCount,
                    await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
                Assert.Equal(1, observer.PreSendCalls);
                Assert.Equal(BoundaryPayload.SerializedLength, observer.FirstBodyLength);
            }

            Assert.Equal(1, converter.WriteCalls);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "raw-json-envelope-cannot-undercut-body")]
    public void RawJsonEnvelopeLimit_CannotBeConfiguredBelowTheBodyLimit()
    {
        ConfigurationException failure = Assert.Throws<ConfigurationException>(() => BuildRawJsonBoundaryProvider(
            new CountingBoundaryPayloadConverter(),
            new BodyReadingObserver(),
            options =>
            {
                options.MaximumSerializedBodyBytes = BoundaryPayload.SerializedLength;
                options.MaximumTransportEnvelopeBytes = BoundaryPayload.SerializedLength - 1;
            },
            _ => { }));

        Assert.Contains(nameof(MessageLimits.MaxEnvelopeBytes), failure.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(MessageLimits.MaxBodyBytes), failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-MESSAGE-DATA", "existing-owner-evidence-allows-admission")]
    public async Task ExistingMessageDataOwner_ProvidesAdmissionEvidenceOnlyAfterAStoredReferenceExistsAsync()
    {
        var repository = new InMemoryMessageDataRepository();
        var received = new TaskCompletionSource<MessageDataSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = BuildMessageDataProvider(repository, received);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        string expected = new('x', 1024);

        await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            await bus.PublishAsync<MessageDataPayload>(new { Value = expected }, TestContext.Current.CancellationToken);
            MessageDataSnapshot snapshot = await received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Assert.NotNull(snapshot.Address);
            Assert.Equal(expected, snapshot.Value);
            Assert.Equal(expected, await (await repository.GetStringAsync(snapshot.Address, TestContext.Current.CancellationToken)).Value);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-MESSAGE-DATA", "configured-owner-without-offload-fails")]
    public async Task ConfiguredMessageDataRepository_WithoutAnActualStoredReferenceDoesNotFakeOffloadAsync()
    {
        var repository = new InMemoryMessageDataRepository();
        var delivered = 0;
        var services = BaseServices();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(new MessageLimits
            {
                MaxBodyBytes = 1_000_000,
                MaxEnvelopeBytes = 1_000_000,
                MaxJsonDepth = 32,
                OffloadToMessageDataAboveBytes = 1,
            });
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://payload-no-fake-offload/"));
                bus.UseMessageData(repository, new MessageDataPolicy(alwaysWriteToRepository: false, threshold: 16));
                bus.ReceiveEndpoint("payload-no-fake-offload-input", endpoint => endpoint.Handler<MessageDataPayload>(context =>
                {
                    Interlocked.Increment(ref delivered);
                    return Task.CompletedTask;
                }));
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        IBusControl control = provider.GetRequiredService<IBusControl>();

        await control.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            PayloadAdmissionException exception = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => control.PublishAsync<MessageDataPayload>(new { Value = "inline" }, TestContext.Current.CancellationToken));

            Assert.Equal(PayloadAdmissionStage.MessageData, exception.Stage);
            Assert.Equal(0, Volatile.Read(ref delivered));
        }
        finally
        {
            await control.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-MULTIBUS", "typed-policies-are-isolated")]
    public async Task TypedBuses_ApplyOnlyTheirOwnPayloadAdmissionPolicyAsync()
    {
        var secondaryReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var services = BaseServices();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(new MessageLimits { MaxBodyBytes = 1, MaxEnvelopeBytes = 1_000_000, MaxJsonDepth = 32 });
            configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://payload-default-bus/")));
        });
        services.AddViciOneServiceBus<ISecondaryBus>(configuration =>
        {
            configuration.Limits(new MessageLimits
            {
                MaxBodyBytes = 1_000_000,
                MaxEnvelopeBytes = 1_000_000,
                MaxJsonDepth = 32,
            });
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://payload-secondary-bus/"));
                bus.ReceiveEndpoint("payload-secondary-input", endpoint => endpoint.Handler<PlainPayload>(context =>
                {
                    secondaryReceived.TrySetResult(context.Message.Value);
                    return Task.CompletedTask;
                }));
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        IBus defaultBus = provider.GetRequiredService<IBus>();
        ISecondaryBus secondaryBus = provider.GetRequiredService<ISecondaryBus>();

        await ((IBusControl)defaultBus).StartAsync(TestContext.Current.CancellationToken)
            .WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await ((IBusControl)secondaryBus).StartAsync(TestContext.Current.CancellationToken)
            .WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            PayloadAdmissionException rejected = await Assert.ThrowsAsync<PayloadAdmissionException>(
                () => defaultBus.PublishAsync(new PlainPayload("default"), TestContext.Current.CancellationToken));
            await secondaryBus.PublishAsync(new PlainPayload("secondary"), TestContext.Current.CancellationToken);

            Assert.Equal(PayloadAdmissionStage.SerializedBody, rejected.Stage);
            Assert.Equal("secondary", await secondaryReceived.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        }
        finally
        {
            await ((IBusControl)secondaryBus).StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            await ((IBusControl)defaultBus).StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
    }

    private static ServiceProvider BuildJsonProvider(
        CountingPayloadConverter converter,
        BodyReadingObserver observer,
        Action<PayloadAdmissionOptions<IBus>> configureAdmission,
        Action<ConsumeContext<CountingPayload>> consume)
    {
        IServiceCollection services = BaseServices();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(CreateLimits(configureAdmission));
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://payload-json/"));
                bus.ConfigureSystemTextJsonSerializerOptions(options =>
                {
                    options.Converters.Add(converter);
                    return options;
                });
                bus.ConnectSendObserver(observer);
                bus.ReceiveEndpoint("payload-json-input", endpoint => endpoint.Handler<CountingPayload>(context =>
                {
                    consume(context);
                    return Task.CompletedTask;
                }));
            });
        });
        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static ServiceProvider BuildRawJsonBoundaryProvider(
        CountingBoundaryPayloadConverter converter,
        BodyReadingObserver observer,
        Action<PayloadAdmissionOptions<IBus>> configureAdmission,
        Action<ConsumeContext<BoundaryPayload>> consume)
    {
        IServiceCollection services = BaseServices();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(CreateLimits(configureAdmission));
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://payload-raw-json/"));
                bus.ConfigureSystemTextJsonSerializerOptions(options =>
                {
                    options.Converters.Add(converter);
                    return options;
                });
                bus.UseRawJsonSerializer(RawSerializerOptions.AddTransportHeaders, true);
                bus.ConnectSendObserver(observer);
                bus.ReceiveEndpoint("payload-raw-json-input", endpoint => endpoint.Handler<BoundaryPayload>(context =>
                {
                    consume(context);
                    return Task.CompletedTask;
                }));
            });
        });
        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static ServiceProvider BuildJsonBoundaryProvider(
        CountingBoundaryPayloadConverter converter,
        BodyReadingObserver observer,
        Action<PayloadAdmissionOptions<IBus>> configureAdmission,
        Action<ConsumeContext<BoundaryPayload>> consume)
    {
        IServiceCollection services = BaseServices();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(CreateLimits(configureAdmission));
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://payload-json-boundary/"));
                bus.ConfigureSystemTextJsonSerializerOptions(options =>
                {
                    options.Converters.Add(converter);
                    return options;
                });
                bus.ConnectSendObserver(observer);
                bus.ReceiveEndpoint("payload-json-boundary-input", endpoint => endpoint.Handler<BoundaryPayload>(context =>
                {
                    consume(context);
                    return Task.CompletedTask;
                }));
            });
        });
        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static async Task SendJsonAsync(IBus bus, CountingPayload message)
    {
        ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri("loopback://payload-json/payload-json-input"));
        await endpoint.SendAsync(message, TestContext.Current.CancellationToken);
    }

    private static async Task SendJsonAsync(IBus bus, BoundaryPayload message)
    {
        ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri("loopback://payload-json-boundary/payload-json-boundary-input"));
        await endpoint.SendAsync(message, TestContext.Current.CancellationToken);
    }

    private static async Task SendRawJsonAsync(IBus bus, BoundaryPayload message)
    {
        ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri("loopback://payload-raw-json/payload-raw-json-input"));
        await endpoint.SendAsync(message, TestContext.Current.CancellationToken);
    }

    private static ServiceProvider BuildMessageDataProvider(
        IMessageDataRepository repository,
        TaskCompletionSource<MessageDataSnapshot> received)
    {
        IServiceCollection services = BaseServices();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(new MessageLimits
            {
                MaxBodyBytes = 1_000_000,
                MaxEnvelopeBytes = 1_000_000,
                MaxJsonDepth = 32,
                OffloadToMessageDataAboveBytes = 1,
            });
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri("loopback://payload-message-data/"));
                bus.UseMessageData(repository, new MessageDataPolicy(alwaysWriteToRepository: false, threshold: 16));
                bus.ReceiveEndpoint("payload-message-data-input", endpoint => endpoint.Handler<MessageDataPayload>(async context =>
                {
                    received.TrySetResult(new MessageDataSnapshot(
                        context.Message.Value.Address
                        ?? throw new Xunit.Sdk.XunitException("Expected the offloaded message-data address to be available."),
                        await context.Message.Value.Value
                        ?? throw new Xunit.Sdk.XunitException("Expected the offloaded message-data value to be available.")));
                }));
            });
        });
        return services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static ServiceCollection BaseServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }

    private static MessageLimits CreateLimits(Action<PayloadAdmissionOptions<IBus>> configure)
    {
        var options = new PayloadAdmissionOptions<IBus>();
        configure(options);
        return new MessageLimits
        {
            MaxBodyBytes = options.MaximumSerializedBodyBytes!.Value,
            MaxEnvelopeBytes = options.MaximumTransportEnvelopeBytes!.Value,
            MaxJsonDepth = 32,
            WarnAboveBytes = options.WarningBodyBytes,
            OffloadToMessageDataAboveBytes = options.MessageDataOffloadThresholdBytes,
        };
    }

    private static void AssertJsonEnvelopeBoundary(int maximumEnvelopeBytes, bool rejected)
    {
        JsonEnvelopeSerializationAttempt attempt = SerializeDeterministicJsonEnvelope(maximumEnvelopeBytes);
        Assert.Equal(1, attempt.ConverterWriteCalls);
        if (rejected)
        {
            Assert.NotNull(attempt.Rejection);
            Assert.Equal(PayloadAdmissionStage.TransportEnvelope, attempt.Rejection.Stage);
            Assert.Null(attempt.Bytes);
            return;
        }

        Assert.Null(attempt.Rejection);
        Assert.NotNull(attempt.Bytes);
        Assert.True(attempt.Bytes.Length <= maximumEnvelopeBytes);
    }

    private static int FindMinimumJsonEnvelopeCapacity(int envelopeLength)
    {
        int rejected = envelopeLength - 1;
        int accepted = checked(envelopeLength * 2);
        Assert.False(CanSerializeJsonEnvelope(rejected));
        Assert.True(CanSerializeJsonEnvelope(accepted));

        while (accepted - rejected > 1)
        {
            int candidate = rejected + ((accepted - rejected) / 2);
            if (CanSerializeJsonEnvelope(candidate))
                accepted = candidate;
            else
                rejected = candidate;
        }

        return accepted;
    }

    private static bool CanSerializeJsonEnvelope(int maximumEnvelopeBytes)
    {
        JsonEnvelopeSerializationAttempt attempt = SerializeDeterministicJsonEnvelope(maximumEnvelopeBytes);
        Assert.Equal(1, attempt.ConverterWriteCalls);
        if (attempt.Rejection is null)
        {
            Assert.NotNull(attempt.Bytes);
            return true;
        }

        Assert.Equal(PayloadAdmissionStage.TransportEnvelope, attempt.Rejection.Stage);
        Assert.Null(attempt.Bytes);
        return false;
    }

    private static JsonEnvelopeSerializationAttempt SerializeDeterministicJsonEnvelope(int maximumEnvelopeBytes)
    {
        var message = new BoundaryPayload(BoundaryPayload.ItemCount);
        var converter = new CountingBoundaryPayloadConverter();
        var options = new JsonSerializerOptions(ServiceBusMetadataJson.Options);
        options.Converters.Add(converter);
        var envelope = new JsonMessageEnvelope
        {
            MessageId = "24736fd8-72c7-44bd-a6a2-6cd7f5496f47",
            RequestId = "4b8a3e69-d269-46ea-b4d5-b5fa373ee4f8",
            CorrelationId = "b91b6ad6-449e-4813-8dde-64de3ece5595",
            ConversationId = "ab92e2f0-2b8a-4ca7-9480-d83814329edb",
            SourceAddress = "loopback://payload-json-boundary/source",
            DestinationAddress = "loopback://payload-json-boundary/input",
            MessageType = [MessageUrn.ForTypeString<BoundaryPayload>()],
            Message = message,
            SentTime = new DateTime(2026, 9, 3, 12, 34, 56, DateTimeKind.Utc),
            Headers = new Dictionary<string, object?>
            {
                ["boundary"] = "deterministic",
            },
        };
        var context = new MessageSendContext<BoundaryPayload>(message);
        var policy = new PayloadAdmissionPolicy
        {
            MaximumSerializedBodyBytes = BoundaryPayload.SerializedLength,
            MaximumTransportEnvelopeBytes = maximumEnvelopeBytes,
        };

        try
        {
            byte[] bytes = PayloadAdmissionSerializationTestDriver.SerializeJsonEnvelope(
                context,
                options,
                envelope,
                policy);
            return new JsonEnvelopeSerializationAttempt(bytes, converter.WriteCalls, null);
        }
        catch (PayloadAdmissionException exception)
        {
            return new JsonEnvelopeSerializationAttempt(null, converter.WriteCalls, exception);
        }
    }

    public interface ISecondaryBus : IBus;

    public sealed record CountingPayload(string Value);

    public sealed record BoundaryPayload(int ValueCount)
    {
        public const int ItemCount = 6_129;
        public const int SerializedLength = 12_287;
    }

    public sealed record PlainPayload(string Value);

    public interface MessageDataPayload
    {
        MessageData<string> Value { get; }
    }

    private sealed record MessageDataSnapshot(Uri Address, string Value);

    private sealed record JsonEnvelopeSerializationAttempt(
        byte[]? Bytes,
        int ConverterWriteCalls,
        PayloadAdmissionException? Rejection);

    private sealed class CountingPayloadConverter : JsonConverter<CountingPayload>
    {
        private int _writeCalls;

        public int WriteCalls => Volatile.Read(ref _writeCalls);

        public override CountingPayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            return new CountingPayload(document.RootElement.GetProperty("value").GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, CountingPayload value, JsonSerializerOptions options)
        {
            Interlocked.Increment(ref _writeCalls);
            writer.WriteStartObject();
            writer.WriteString("value", value.Value);
            writer.WriteEndObject();
        }
    }

    private sealed class CountingBoundaryPayloadConverter : JsonConverter<BoundaryPayload>
    {
        private int _writeCalls;

        public int WriteCalls => Volatile.Read(ref _writeCalls);

        public override BoundaryPayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            return new BoundaryPayload(document.RootElement.GetProperty("value").GetArrayLength());
        }

        public override void Write(Utf8JsonWriter writer, BoundaryPayload value, JsonSerializerOptions options)
        {
            Interlocked.Increment(ref _writeCalls);
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteStartArray();
            for (var index = 0; index < value.ValueCount - 1; index++)
                writer.WriteNumberValue(0);
            writer.WriteNumberValue(1_234_567_890_123_456_789L);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }

    private sealed class BodyReadingObserver : ISendObserver
    {
        private int _preSendCalls;

        public int PreSendCalls => Volatile.Read(ref _preSendCalls);

        public long? FirstBodyLength { get; private set; }

        public long? SecondBodyLength { get; private set; }

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            Interlocked.Increment(ref _preSendCalls);
            TransportSendContext transport = Assert.IsAssignableFrom<TransportSendContext>(context);
            FirstBodyLength = transport.Body.GetBytes().LongLength;
            SecondBodyLength = transport.Body.GetString().Length > 0
                ? transport.Body.GetBytes().LongLength
                : null;
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
