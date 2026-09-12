using Microsoft.AspNetCore.SignalR.Protocol;
using ViciOne.ServiceBus.SignalR.Contracts;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class BackplaneContractValidationTests
{
    private static readonly IReadOnlyDictionary<string, byte[]> Payloads =
        new Dictionary<string, byte[]> { ["json"] = [1] };

    [Fact]
    public void FanOutContracts_RequireTheirRoutingAndPayloadMembers()
    {
        AssertMissingArgument(
            () => new BroadcastMessage<TestHub>(null!, []),
            "protocolPayloads");
        AssertMissingArgument(
            () => new BroadcastMessage<TestHub>(Payloads, null!),
            "excludedConnectionIds");
        AssertMissingArgument(
            () => new GroupMessage<TestHub>(null!, Payloads, []),
            "groupName");
        AssertMissingArgument(
            () => new GroupMessage<TestHub>("group", null!, []),
            "protocolPayloads");
        AssertMissingArgument(
            () => new GroupMessage<TestHub>("group", Payloads, null!),
            "excludedConnectionIds");
        AssertMissingArgument(
            () => new UserMessage<TestHub>(null!, Payloads),
            "userId");
        AssertMissingArgument(
            () => new UserMessage<TestHub>("user", null!),
            "protocolPayloads");
    }

    [Fact]
    public void ConnectionContracts_RequireTheirRoutingAndPayloadMembers()
    {
        AssertMissingArgument(
            () => new ConnectionMessage<TestHub>(null!, Payloads),
            "connectionId");
        AssertMissingArgument(
            () => new ConnectionMessage<TestHub>("connection", null!),
            "protocolPayloads");
        AssertMissingArgument(
            () => new InvocationCancellationMessage<TestHub>(null!, "invocation"),
            "connectionId");
        AssertMissingArgument(
            () => new InvocationCancellationMessage<TestHub>("connection", null!),
            "invocationId");
    }

    [Fact]
    public void ClientResultContract_RequiresEveryEnvelopeMember()
    {
        AssertMissingArgument(
            () => new ClientResultMessage<TestHub>(null!, "invocation", "json", [1]),
            "targetNodeId");
        AssertMissingArgument(
            () => new ClientResultMessage<TestHub>("node", null!, "json", [1]),
            "invocationId");
        AssertMissingArgument(
            () => new ClientResultMessage<TestHub>("node", "invocation", null!, [1]),
            "protocolName");
        AssertMissingArgument(
            () => new ClientResultMessage<TestHub>("node", "invocation", "json", null!),
            "payload");
    }

    [Fact]
    public void GroupCommandContracts_RequireTheirAddressAndAcknowledgementMembers()
    {
        AssertMissingArgument(
            () => new GroupCommand<TestHub>(GroupCommandAction.Add, null!, "connection"),
            "groupName");
        AssertMissingArgument(
            () => new GroupCommand<TestHub>(GroupCommandAction.Add, "group", null!),
            "connectionId");
        AssertMissingArgument(
            () => new GroupCommandAcknowledgement<TestHub>(null!),
            "nodeId");
    }

    [Fact]
    public void ValidContracts_PreserveEverySuppliedMember()
    {
        var broadcast = new BroadcastMessage<TestHub>(Payloads, ["excluded"]);
        var connection = new ConnectionMessage<TestHub>("connection", Payloads, "invocation", "node");
        var group = new GroupMessage<TestHub>("group", Payloads, ["excluded"]);
        var user = new UserMessage<TestHub>("user", Payloads);
        var command = new GroupCommand<TestHub>(GroupCommandAction.Remove, "group", "connection");
        var acknowledgement = new GroupCommandAcknowledgement<TestHub>("node");
        var cancellation = new InvocationCancellationMessage<TestHub>("connection", "invocation");
        var result = new ClientResultMessage<TestHub>("node", "invocation", "json", [1]);

        Assert.Same(Payloads, broadcast.ProtocolPayloads);
        Assert.Equal(["excluded"], broadcast.ExcludedConnectionIds);
        Assert.Equal(("connection", "invocation", "node"),
            (connection.ConnectionId, connection.InvocationId, connection.ResultNodeId));
        Assert.Same(Payloads, connection.ProtocolPayloads);
        Assert.Equal("group", group.GroupName);
        Assert.Same(Payloads, group.ProtocolPayloads);
        Assert.Equal(["excluded"], group.ExcludedConnectionIds);
        Assert.Equal("user", user.UserId);
        Assert.Same(Payloads, user.ProtocolPayloads);
        Assert.Equal((GroupCommandAction.Remove, "group", "connection"),
            (command.Action, command.GroupName, command.ConnectionId));
        Assert.Equal("node", acknowledgement.NodeId);
        Assert.Equal(("connection", "invocation"),
            (cancellation.ConnectionId, cancellation.InvocationId));
        Assert.Equal(("node", "invocation", "json"),
            (result.TargetNodeId, result.InvocationId, result.ProtocolName));
        Assert.Equal([1], result.Payload);
    }

    private static void AssertMissingArgument(Action action, string parameterName)
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(parameterName, exception.ParamName);
    }
}
