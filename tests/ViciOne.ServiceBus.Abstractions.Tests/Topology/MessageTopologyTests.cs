using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class MessageTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ROOTS", "generic-invalid-message-contracts")]
    public void GenericRootLookups_RejectInvalidMessageContracts()
    {
        var message = new MessageTopology(new ConstantEntityNameFormatter("message"));
        var publish = new PublishTopology();
        var send = new SendTopology();

        Assert.Equal("T", Assert.Throws<ArgumentException>(
            () => ((IMessageTopologyConfigurator)message).GetMessageTopology<string>()).ParamName);
        Assert.Equal("T", Assert.Throws<ArgumentException>(
            () => ((IPublishTopologyConfigurator)publish).GetMessageTopology<string>()).ParamName);
        Assert.Equal("T", Assert.Throws<ArgumentException>(
            () => send.GetMessageTopology<string>()).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "root-formatter-snapshot-per-message-contract")]
    public void RootFormatterReplacement_AffectsOnlySubsequentlyCreatedMessageTopologies()
    {
        var topology = new MessageTopology(new ConstantEntityNameFormatter("first"));
        IMessageTopologyConfigurator<Message> first =
            ((IMessageTopologyConfigurator)topology).GetMessageTopology<Message>();

        topology.SetEntityNameFormatter(new ConstantEntityNameFormatter("second"));
        IMessageTopologyConfigurator<SecondMessage> second =
            ((IMessageTopologyConfigurator)topology).GetMessageTopology<SecondMessage>();

        Assert.Equal("first", first.EntityName);
        Assert.Equal("second", second.EntityName);
        Assert.Same(first, ((IMessageTopologyConfigurator)topology).GetMessageTopology<Message>());
        Assert.Same(first, ((IMessageTopology)topology).GetMessageTopology<Message>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "message-formatter-before-and-after-evaluation")]
    public void MessageFormatter_CanChangeBeforeEvaluationButNotToADifferentEvaluatedName()
    {
        var topology = new MessageTopology<Message>(new ConstantMessageEntityNameFormatter<Message>("initial"));
        topology.SetEntityNameFormatter(new ConstantMessageEntityNameFormatter<Message>("selected"));

        Assert.Equal("selected", topology.EntityName);
        topology.SetEntityNameFormatter(new ConstantMessageEntityNameFormatter<Message>("selected"));
        Assert.Equal("selected", topology.EntityName);

        ConfigurationException exception = Assert.Throws<ConfigurationException>(
            () => topology.SetEntityNameFormatter(new ConstantMessageEntityNameFormatter<Message>("different")));
        Assert.Contains("already evaluated: selected", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "message-fixed-and-invalid-names")]
    public void MessageEntityName_RequiresANonEmptyValueFromEveryConfigurationPath()
    {
        var fixedName = new MessageTopology<Message>(new ConstantMessageEntityNameFormatter<Message>("initial"));
        fixedName.SetEntityName("orders");
        Assert.Equal("orders", fixedName.EntityName);

        Assert.Equal("entityName", Assert.ThrowsAny<ArgumentException>(
            () => fixedName.SetEntityName(" ")).ParamName);

        var invalidFormatter = new MessageTopology<Message>(new ConstantMessageEntityNameFormatter<Message>(" "));
        Assert.Equal("EntityNameFormatter", Assert.ThrowsAny<ArgumentException>(
            () => _ = invalidFormatter.EntityName).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-NAME", "message-concurrent-single-evaluation")]
    public async Task MessageEntityName_EvaluatesItsFormatterExactlyOnceUnderConcurrencyAsync()
    {
        var formatter = new CoordinatedMessageEntityNameFormatter();
        var topology = new MessageTopology<Message>(formatter);

        Task<string> first = Task.Run(() => topology.EntityName);
        Assert.True(formatter.FirstInvocation.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
        Task<string> second = Task.Run(() => topology.EntityName);
        formatter.SecondInvocation.Wait(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken);
        formatter.Release.Set();

        string[] values = await Task.WhenAll(first, second);

        Assert.Equal(["message", "message"], values);
        Assert.Equal(1, formatter.InvocationCount);
    }

    private sealed record Message;

    private sealed record SecondMessage;

    private sealed class ConstantEntityNameFormatter(string value) : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => value;
    }

    private sealed class ConstantMessageEntityNameFormatter<TMessage>(string value) : IMessageEntityNameFormatter<TMessage>
        where TMessage : class
    {
        public string FormatEntityName() => value;
    }

    private sealed class CoordinatedMessageEntityNameFormatter : IMessageEntityNameFormatter<Message>
    {
        int _invocationCount;

        internal ManualResetEventSlim FirstInvocation { get; } = new();

        internal int InvocationCount => Volatile.Read(ref _invocationCount);

        internal ManualResetEventSlim Release { get; } = new();

        internal ManualResetEventSlim SecondInvocation { get; } = new();

        public string FormatEntityName()
        {
            int invocation = Interlocked.Increment(ref _invocationCount);
            if (invocation == 1)
                FirstInvocation.Set();
            else
                SecondInvocation.Set();
            Assert.True(Release.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
            return "message";
        }
    }
}
