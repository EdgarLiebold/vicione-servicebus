using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class ConfiguredJsonCopyAdmissionTests
{
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 0)]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(false, false, 2)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 2)]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "configured-copy-envelope-exact-boundaries")]
    public async Task ConfiguredEnvelope_ChargesTheExactApplicationValueAndDeliversOnlyAdmittedBytesAsync(
        bool removeAttributeProvider, bool upperCase, int rejectedStage)
    {
        const string Payload = """{"value":"é","payload":{"value":"nested-decoy"}}""";
        Guid candidateId = NewId.NextGuid();
        string propertyName = removeAttributeProvider ? "payload" : "content";
        string envelope = Envelope(candidateId, $"\"{(upperCase ? propertyName.ToUpperInvariant() : propertyName)}\":{Payload}");
        int bodyBytes = Encoding.UTF8.GetByteCount(Payload);
        int envelopeBytes = Encoding.UTF8.GetByteCount(envelope);
        var state = new AdmissionState();
        await using ServiceProvider provider = BuildProvider(state, removeAttributeProvider, caseInsensitive: true,
            bodyBytes - (rejectedStage == 1 ? 1 : 0), envelopeBytes - (rejectedStage == 2 ? 1 : 0), overrideNaming: true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        try
        {
            await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri(bus.Address, "input"), TestContext.Current.CancellationToken);
            if (rejectedStage == 0)
            {
                await SendAsync(endpoint, candidateId, envelope);
                Assert.Equal((candidateId, "é"), await state.Received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
                Assert.Equal(Encoding.UTF8.GetBytes(envelope), Assert.Single(state.Admitted).Bytes);
            }
            else
            {
                PayloadAdmissionException failure = await Assert.ThrowsAsync<PayloadAdmissionException>(
                    () => SendAsync(endpoint, candidateId, envelope));
                Assert.Equal(rejectedStage == 1 ? PayloadAdmissionStage.SerializedBody : PayloadAdmissionStage.TransportEnvelope, failure.Stage);
                int expectedBytes = rejectedStage == 1 ? bodyBytes : envelopeBytes;
                Assert.Equal(expectedBytes, failure.ActualBytes);
                Assert.Equal(expectedBytes - 1, failure.ConfiguredLimitBytes);
                Assert.Empty(state.Admitted);
                Assert.Empty(state.Deliveries);
                Guid healthyId = NewId.NextGuid();
                string healthyEnvelope = Envelope(healthyId, $"\"{propertyName}\":{{\"value\":\"ok\"}}", padding: false);
                await SendAsync(endpoint, healthyId, healthyEnvelope);
                Assert.Equal((healthyId, "ok"), await state.Received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
                Assert.Equal(healthyId, Assert.Single(state.Admitted).Id);
                Assert.Equal(Encoding.UTF8.GetBytes(healthyEnvelope), Assert.Single(state.Admitted).Bytes);
            }
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
        Assert.Single(state.Admitted);
        Assert.Single(state.Deliveries);
    }

    [Theory]
    [InlineData("\"payload\":{\"value\":\"one\"},\"payload\":{\"value\":\"two\"}", true, "no unique message value")]
    [InlineData("\"payload\":{\"value\":\"one\"},\"PAYLOAD\":{\"value\":\"two\"}", true, "no unique message value")]
    [InlineData("\"other\":{\"payload\":{\"value\":\"nested-only\"}}", true, "no message value")]
    [InlineData("\"PAYLOAD\":{\"value\":\"wrong-case\"}", false, "no message value")]
    [InlineData("[]", true, "not an object")]
    [InlineData("null", true, "not an object")]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-RUNTIME", "ambiguous-json-copy-rejected-before-provider")]
    public async Task AmbiguousEnvelope_IsRejectedBeforeSendAdmissionAndLeavesTheNextMessageOperationalAsync(
        string properties, bool caseInsensitive, string expectedFailure)
    {
        var state = new AdmissionState();
        await using ServiceProvider provider = BuildProvider(state, removeAttributeProvider: false, caseInsensitive, 1024, 4096);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        try
        {
            await bus.StartAsync(TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri(bus.Address, "input"), TestContext.Current.CancellationToken);
            Guid rejectedId = NewId.NextGuid();
            string envelope = properties is "[]" or "null" ? properties : Envelope(rejectedId, properties);
            InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
                () => SendAsync(endpoint, rejectedId, envelope));
            Assert.Contains(expectedFailure, failure.Message, StringComparison.Ordinal);
            Assert.Empty(state.Admitted);
            Assert.Empty(state.Deliveries);

            Guid healthyId = NewId.NextGuid();
            await SendAsync(endpoint, healthyId, Envelope(healthyId, "\"payload\":{\"value\":\"healthy\"}"));
            Assert.Equal((healthyId, "healthy"), await state.Received.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Equal(healthyId, Assert.Single(state.Admitted).Id);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
        Assert.Single(state.Admitted);
        Assert.Single(state.Deliveries);
    }

    private static ServiceProvider BuildProvider(AdmissionState state, bool removeAttributeProvider,
        bool caseInsensitive, int bodyLimit, int envelopeLimit, bool overrideNaming = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(new MessageLimits { MaxBodyBytes = bodyLimit, MaxEnvelopeBytes = envelopeLimit, MaxJsonDepth = 32 });
            configuration.UsingInMemory((_, bus) =>
            {
                bus.Host(new Uri($"loopback://json-copy-{NewId.NextGuid():N}/"));
                bus.ConfigureSystemTextJsonSerializerOptions(options =>
                {
                    options.PropertyNameCaseInsensitive = caseInsensitive;
                    options.PropertyNamingPolicy = new EnvelopeNamingPolicy();
                    var resolver = new DefaultJsonTypeInfoResolver();
                    resolver.Modifiers.Add(type =>
                    {
                        if (type.Type != typeof(JsonMessageEnvelope))
                            return;
                        JsonPropertyInfo message = type.Properties.Single(property =>
                            property.AttributeProvider is MemberInfo member && member.Name == nameof(JsonMessageEnvelope.Message));
                        message.Name = overrideNaming && !removeAttributeProvider ? "content" : "payload";
                        if (removeAttributeProvider)
                            message.AttributeProvider = null;
                    });
                    options.TypeInfoResolver = resolver;
                    return options;
                });
                bus.ConnectSendObserver(state);
                bus.ReceiveEndpoint("input", endpoint => endpoint.Handler<CopyMessage>(context =>
                {
                    var delivery = (context.MessageId!.Value, context.Message.Value);
                    state.Deliveries.Enqueue(delivery);
                    state.Received.TrySetResult(delivery);
                    return Task.CompletedTask;
                }));
            });
        });
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static string Envelope(Guid id, string properties, bool padding = true) => $$"""
        {"messageId":"{{id:D}}","messageTypes":["{{MessageUrn.ForTypeString<CopyMessage>()}}"],
         "headers":{"decoy":{"payload":"ignore-me"},"padding":"{{(padding ? new string('x', 512) : "")}}"},{{properties}}}
        """;

    private static Task SendAsync(ISendEndpoint endpoint, Guid id, string envelope) => endpoint.SendAsync(new CopyMessage("placeholder"), context =>
    {
        context.MessageId = id;
        context.Serializer = new CopyBodySerializer(SystemTextJsonMessageSerializer.JsonContentType, new StringMessageBody(envelope));
    }, TestContext.Current.CancellationToken).WaitAsync(Timeout, TestContext.Current.CancellationToken);

    public sealed record CopyMessage(string Value);

    private sealed class EnvelopeNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name == nameof(JsonMessageEnvelope.Message) ? "payload" : CamelCase.ConvertName(name);
    }

    private sealed class AdmissionState : ISendObserver
    {
        public ConcurrentQueue<(Guid? Id, byte[] Bytes)> Admitted { get; } = new();
        public ConcurrentQueue<(Guid Id, string Value)> Deliveries { get; } = new();
        public TaskCompletionSource<(Guid Id, string Value)> Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            if (typeof(T) == typeof(CopyMessage))
                Admitted.Enqueue((context.MessageId, Assert.IsAssignableFrom<TransportSendContext>(context).Body.ToArray()));
            return Task.CompletedTask;
        }
        public Task PostSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;
        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }
}
