using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class SendTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ROOTS", "send-convention-current-future-and-duplicate")]
    public void RootConvention_AppliesOnceToExistingAndFutureMessageTopologies()
    {
        var trace = new List<string>();
        var topology = new SendTopology();
        IMessageSendTopologyConfigurator<Message> existing = topology.GetMessageTopology<Message>();
        var convention = new RecordingRootConvention(trace);

        Assert.True(topology.TryAddConvention(convention));
        Assert.False(topology.TryAddConvention(new RecordingRootConvention(trace)));

        IMessageSendTopologyConfigurator<SecondMessage> future = topology.GetMessageTopology<SecondMessage>();
        existing.Apply(new RecordingBuilder<SendContext<Message>>());
        future.Apply(new RecordingBuilder<SendContext<SecondMessage>>());

        Assert.Equal(["Message", "SecondMessage"], trace.OrderBy(value => value));
        Assert.Empty(topology.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ROOTS", "send-explicit-topology-and-formatters")]
    public void RootConfiguration_AddsExplicitTopologyAndOwnsItsFailureQueueFormatters()
    {
        var trace = new List<string>();
        var topology = new SendTopology();
        var error = new ErrorFormatter();
        var deadLetter = new DeadLetterFormatter();
        topology.ErrorQueueNameFormatter = error;
        topology.DeadLetterQueueNameFormatter = deadLetter;

        ((ISendTopologyConfigurator)topology).AddMessageSendTopology<Message>(new RecordingTopology<Message>(trace));
        topology.GetMessageTopology<Message>().Apply(new RecordingBuilder<SendContext<Message>>());

        Assert.Equal(["Message"], trace);
        Assert.Same(error, topology.ErrorQueueNameFormatter);
        Assert.Same(deadLetter, topology.DeadLetterQueueNameFormatter);
        Assert.Equal("orders.failed", topology.ErrorQueueNameFormatter.FormatErrorQueueName("orders"));
        Assert.Equal("orders.dead", topology.DeadLetterQueueNameFormatter.FormatDeadLetterQueueName("orders"));
    }

    private sealed record Message;

    private sealed record SecondMessage;

    private sealed class RecordingRootConvention(List<string> trace) : ISendTopologyConvention
    {
        public bool TryGetMessageSendTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
            where T : class
        {
            convention = new RecordingConvention<T>(trace);
            return true;
        }
    }

    private sealed class RecordingConvention<TMessage>(List<string> trace) : IMessageSendTopologyConvention<TMessage>
        where TMessage : class
    {
        public bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
        {
            messageSendTopology = new RecordingTopology<TMessage>(trace);
            return true;
        }

        public bool TryGetMessageSendTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
            where T : class
        {
            convention = this as IMessageSendTopologyConvention<T>;
            return convention is not null;
        }
    }

    private sealed class RecordingTopology<TMessage>(List<string> trace) : IMessageSendTopology<TMessage>
        where TMessage : class
    {
        public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
        {
            trace.Add(typeof(TMessage).Name);
        }
    }

    private sealed class RecordingBuilder<TContext> : ITopologyPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        public bool IsDelegated => false;

        public bool IsImplemented => false;

        public void AddFilter(IFilter<TContext> filter)
        {
        }

        public ITopologyPipeBuilder<TContext> CreateDelegatedBuilder() => new RecordingBuilder<TContext>();
    }

    private sealed class ErrorFormatter : IErrorQueueNameFormatter
    {
        public string FormatErrorQueueName(string queueName) => $"{queueName}.failed";
    }

    private sealed class DeadLetterFormatter : IDeadLetterQueueNameFormatter
    {
        public string FormatDeadLetterQueueName(string queueName) => $"{queueName}.dead";
    }
}
