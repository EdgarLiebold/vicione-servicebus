using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class MessageSendTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-SEND-TOPOLOGY", "delegated-convention-explicit-order")]
    public void Apply_ComposesDelegatedConventionAndExplicitTopologyInOrder()
    {
        var trace = new List<string>();
        var topology = new MessageSendTopology<Message>();
        topology.AddDelegate(new RecordingTopology<Message>(trace, "delegated"));
        topology.TryAddConvention(new MissingConvention<Message>());
        topology.TryAddConvention(new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "convention")));
        topology.Add(new RecordingTopology<Message>(trace, "explicit"));

        topology.Apply(new RecordingBuilder<SendContext<Message>>(trace));

        Assert.Equal(
        [
            "create-delegated",
            "delegated:delegated",
            "convention:direct",
            "explicit:direct"
        ], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-SEND-TOPOLOGY", "convention-lifecycle")]
    public void ConventionLifecycle_AddsFindsRejectsDuplicatesUpdatesAndCreatesExactlyAsRequested()
    {
        var trace = new List<string>();
        var topology = new MessageSendTopology<Message>();
        Assert.False(topology.TryGetConvention(out RecordingConvention<Message>? missing));
        Assert.Null(missing);

        var first = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "first"));
        var duplicate = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "duplicate"));
        Assert.True(topology.TryAddConvention(first));
        Assert.False(topology.TryAddConvention(duplicate));
        Assert.True(topology.TryGetConvention(out RecordingConvention<Message>? found));
        Assert.Same(first, found);

        var replacement = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "replacement"));
        topology.UpdateConvention<RecordingConvention<Message>>(existing =>
        {
            Assert.Same(first, existing);
            return replacement;
        });

        var final = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "final"));
        topology.AddOrUpdateConvention<RecordingConvention<Message>>(
            () => throw new InvalidOperationException("The add path must not run."),
            existing =>
            {
                Assert.Same(replacement, existing);
                return final;
            });
        topology.Apply(new RecordingBuilder<SendContext<Message>>(trace));
        Assert.Equal(["create-delegated", "final:direct"], trace);

        var empty = new MessageSendTopology<Message>();
        bool updateCalled = false;
        empty.UpdateConvention<RecordingConvention<Message>>(existing =>
        {
            updateCalled = true;
            return existing;
        });
        Assert.False(updateCalled);

        var added = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "added"));
        empty.AddOrUpdateConvention<RecordingConvention<Message>>(
            () => added,
            _ => throw new InvalidOperationException("The update path must not run."));
        empty.Apply(new RecordingBuilder<SendContext<Message>>(trace));
        Assert.Equal(
            ["create-delegated", "final:direct", "create-delegated", "added:direct"],
            trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-SEND-TOPOLOGY", "root-convention-projection")]
    public void RootConvention_IsAddedOnlyWhenItProjectsToTheMessageContract()
    {
        var trace = new List<string>();
        var topology = new MessageSendTopology<Message>();

        Assert.False(topology.TryAddConvention(new MissingRootConvention()));
        Assert.True(topology.TryAddConvention(new RecordingRootConvention(trace)));
        topology.Apply(new RecordingBuilder<SendContext<Message>>(trace));

        Assert.Equal(["create-delegated", "root-Message:direct"], trace);
        Assert.Empty(topology.Validate());
    }

    private sealed record Message;

    private sealed class RecordingTopology<TMessage>(List<string> trace, string marker) : IMessageSendTopology<TMessage>
        where TMessage : class
    {
        public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
        {
            trace.Add($"{marker}:{(builder.IsDelegated ? "delegated" : "direct")}");
        }
    }

    private sealed class RecordingConvention<TMessage>(IMessageSendTopology<TMessage> topology) :
        IMessageSendTopologyConvention<TMessage>
        where TMessage : class
    {
        public bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
        {
            messageSendTopology = topology;
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

    private sealed class MissingConvention<TMessage> : IMessageSendTopologyConvention<TMessage>
        where TMessage : class
    {
        public bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
        {
            messageSendTopology = null;
            return false;
        }

        public bool TryGetMessageSendTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
            where T : class
        {
            convention = null;
            return false;
        }
    }

    private sealed class MissingRootConvention : ISendTopologyConvention
    {
        public bool TryGetMessageSendTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
            where T : class
        {
            convention = null;
            return false;
        }
    }

    private sealed class RecordingRootConvention(List<string> trace) : ISendTopologyConvention
    {
        public bool TryGetMessageSendTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
            where T : class
        {
            convention = new RecordingConvention<T>(new RecordingTopology<T>(trace, $"root-{typeof(T).Name}"));
            return true;
        }
    }

    private sealed class RecordingBuilder<TContext>(List<string> trace, bool isDelegated = false) : ITopologyPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        public bool IsDelegated { get; } = isDelegated;

        public bool IsImplemented => false;

        public void AddFilter(IFilter<TContext> filter)
        {
        }

        public ITopologyPipeBuilder<TContext> CreateDelegatedBuilder()
        {
            trace.Add("create-delegated");
            return new RecordingBuilder<TContext>(trace, true);
        }
    }
}
