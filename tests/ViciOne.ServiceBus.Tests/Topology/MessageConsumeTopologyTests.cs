using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology;

public sealed class MessageConsumeTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-TOPOLOGY", "delegated-convention-explicit-order")]
    public void Apply_ComposesDelegatedConventionAndExplicitTopologyInOrder()
    {
        var trace = new List<string>();
        var topology = new MessageConsumeTopology<Message>();
        topology.AddDelegate(new RecordingTopology<Message>(trace, "delegated"));
        topology.TryAddConvention(new MissingConvention<Message>());
        topology.TryAddConvention(new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "convention")));
        topology.Add(new RecordingTopology<Message>(trace, "explicit"));

        topology.Apply(new RecordingBuilder<ConsumeContext<Message>>(trace));

        Assert.Equal(
        [
            "create-delegated",
            "delegated:delegated",
            "convention:direct",
            "explicit:direct"
        ], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-TOPOLOGY", "convention-lifecycle")]
    public void ConventionLifecycle_AddsRejectsDuplicatesUpdatesAndCreatesExactlyAsRequested()
    {
        var trace = new List<string>();
        var topology = new MessageConsumeTopology<Message>();
        var first = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "first"));
        var duplicate = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "duplicate"));

        Assert.True(topology.TryAddConvention(first));
        Assert.False(topology.TryAddConvention(duplicate));

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

        topology.Apply(new RecordingBuilder<ConsumeContext<Message>>(trace));

        Assert.Equal(["final:direct"], trace);

        var empty = new MessageConsumeTopology<Message>();
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
        empty.Apply(new RecordingBuilder<ConsumeContext<Message>>(trace));

        Assert.Equal(["final:direct", "added:direct"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-TOPOLOGY", "root-convention-projection")]
    public void RootConvention_IsAddedOnlyWhenItProjectsToTheMessageContract()
    {
        var trace = new List<string>();
        var topology = new MessageConsumeTopology<Message>();

        Assert.False(topology.TryAddConvention(new MissingRootConvention()));
        Assert.True(topology.TryAddConvention(new RecordingRootConvention(trace)));

        topology.Apply(new RecordingBuilder<ConsumeContext<Message>>(trace));

        Assert.Equal(["root-Message:direct"], trace);
        Assert.Empty(topology.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-TOPOLOGY", "application-wide-bindable-contract-classification")]
    public void BindableClassification_UsesTheApplicationMessageContractPolicy()
    {
        Assert.True(new InspectableMessageConsumeTopology<Message>().Bindable);
        Assert.False(new InspectableMessageConsumeTopology<TestAssemblyBootstrap.NonConsumableTopologyMessage>().Bindable);
    }

    private sealed record Message;

    private sealed class InspectableMessageConsumeTopology<TMessage> : MessageConsumeTopology<TMessage>
        where TMessage : class
    {
        internal bool Bindable => IsBindableMessageType;
    }

    private sealed class RecordingTopology<TMessage>(List<string> trace, string marker) : IMessageConsumeTopology<TMessage>
        where TMessage : class
    {
        public void Apply(ITopologyPipeBuilder<ConsumeContext<TMessage>> builder)
        {
            trace.Add($"{marker}:{(builder.IsDelegated ? "delegated" : "direct")}");
        }
    }

    private sealed class RecordingConvention<TMessage>(IMessageConsumeTopology<TMessage> topology) :
        IMessageConsumeTopologyConvention<TMessage>
        where TMessage : class
    {
        public bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? messageConsumeTopology)
        {
            messageConsumeTopology = topology;
            return true;
        }

        public bool TryGetMessageConsumeTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
            where T : class
        {
            convention = this as IMessageConsumeTopologyConvention<T>;
            return convention is not null;
        }
    }

    private sealed class MissingConvention<TMessage> : IMessageConsumeTopologyConvention<TMessage>
        where TMessage : class
    {
        public bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? messageConsumeTopology)
        {
            messageConsumeTopology = null;
            return false;
        }

        public bool TryGetMessageConsumeTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
            where T : class
        {
            convention = null;
            return false;
        }
    }

    private sealed class MissingRootConvention : IConsumeTopologyConvention
    {
        public bool TryGetMessageConsumeTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
            where T : class
        {
            convention = null;
            return false;
        }
    }

    private sealed class RecordingRootConvention(List<string> trace) : IConsumeTopologyConvention
    {
        public bool TryGetMessageConsumeTopologyConvention<T>(
            [NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
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
