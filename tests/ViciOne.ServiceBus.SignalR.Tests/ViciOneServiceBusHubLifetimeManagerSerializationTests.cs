using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.SignalR.Contracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class ViciOneServiceBusHubLifetimeManagerSerializationTests : IAsyncLifetime
{
    private HubLifetimeManagerTestEnvironment<TestHub> _environment = null!;

    public async ValueTask InitializeAsync()
    {
        _environment = await HubLifetimeManagerTestEnvironment<TestHub>.StartAsync(
            2,
            index => index == 0
                ?
                [
                    new JsonHubProtocol(Options.Create(new JsonHubProtocolOptions
                    {
                        PayloadSerializerOptions = { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                    })),
                    new MessagePackHubProtocol(Options.Create(new MessagePackHubProtocolOptions())),
                ]
                :
                [
                    new JsonHubProtocol(),
                    new MessagePackHubProtocol(Options.Create(new MessagePackHubProtocolOptions())),
                ]).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => _environment.DisposeAsync();

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SERIALIZATION", "origin-json-policy-crosses-backplane")]
    public async Task SendAll_PreservesTheOriginatingJsonNamingPolicyAcrossTheBackplaneAsync()
    {
        var first = _environment.Endpoints[0];
        var second = _environment.Endpoints[1];
        await using var receivingClient = new HubConnectionTestClient();
        await second.Manager.OnConnectedAsync(receivingClient.HubConnection);

        await first.Manager.SendAllAsync(
            "Hello",
            [new TestPayload { TestProperty = "value" }],
            TestContext.Current.CancellationToken);

        Assert.True(await second.All.Consumed.AnyAsync<All<TestHub>>(TestContext.Current.CancellationToken));
        var invocation = await receivingClient.ReadInvocationAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.Equal("Hello", invocation.Target);
        var argument = Assert.IsType<JsonElement>(Assert.Single(invocation.Arguments));
        Assert.Equal("value", argument.GetProperty("testProperty").GetString());
        Assert.False(argument.TryGetProperty("TestProperty", out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SIGNALR-SERIALIZATION", "messagepack-crosses-backplane")]
    public async Task SendAll_PreservesMessagePackInvocationsAcrossTheBackplaneAsync()
    {
        var first = _environment.Endpoints[0];
        var second = _environment.Endpoints[1];
        await using var receivingClient = new HubConnectionTestClient(
            protocol: new MessagePackHubProtocol(Options.Create(new MessagePackHubProtocolOptions())));
        await second.Manager.OnConnectedAsync(receivingClient.HubConnection);

        await first.Manager.SendAllAsync(
            "Hello",
            ["World"],
            TestContext.Current.CancellationToken);

        Assert.True(await second.All.Consumed.AnyAsync<All<TestHub>>(TestContext.Current.CancellationToken));
        var invocation = await receivingClient.ReadInvocationAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        Assert.Equal("Hello", invocation.Target);
        Assert.Equal("World", Assert.Single(invocation.Arguments));
    }

    public sealed class TestPayload
    {
        public required string TestProperty { get; init; }
    }
}
