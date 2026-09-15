using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class MessagePublishTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PUBLISH-TOPOLOGY", "delegated-convention-explicit-order")]
    public void Apply_ComposesDelegatedConventionAndExplicitTopologyInOrder()
    {
        var trace = new List<string>();
        var topology = new MessagePublishTopology<Message>(new PublishTopology());
        topology.AddDelegate(new RecordingTopology<Message>(trace, "delegated"));
        topology.TryAddConvention(new MissingConvention<Message>());
        topology.TryAddConvention(new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "convention")));
        topology.Add(new RecordingTopology<Message>(trace, "explicit"));

        topology.Apply(new RecordingBuilder<PublishContext<Message>>(trace));

        Assert.Equal(
        [
            "create-delegated",
            "delegated:delegated",
            "convention:direct",
            "explicit:direct"
        ], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PUBLISH-TOPOLOGY", "convention-lifecycle")]
    public void ConventionLifecycle_AddsRejectsDuplicatesUpdatesAndCreatesExactlyAsRequested()
    {
        var trace = new List<string>();
        var topology = new MessagePublishTopology<Message>(new PublishTopology());
        var first = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "first"));
        var duplicate = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "duplicate"));

        Assert.True(topology.TryAddConvention(first));
        Assert.False(topology.TryAddConvention(duplicate));

        var replacement = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "replacement"));
        topology.AddOrUpdateConvention<RecordingConvention<Message>>(
            () => throw new InvalidOperationException("The add path must not run."),
            existing =>
            {
                Assert.Same(first, existing);
                return replacement;
            });
        topology.Apply(new RecordingBuilder<PublishContext<Message>>(trace));

        Assert.Equal(["create-delegated", "replacement:direct"], trace);

        var empty = new MessagePublishTopology<Message>(new PublishTopology());
        var added = new RecordingConvention<Message>(new RecordingTopology<Message>(trace, "added"));
        empty.AddOrUpdateConvention<RecordingConvention<Message>>(
            () => added,
            _ => throw new InvalidOperationException("The update path must not run."));
        empty.Apply(new RecordingBuilder<PublishContext<Message>>(trace));

        Assert.Equal(
            ["create-delegated", "replacement:direct", "create-delegated", "added:direct"],
            trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PUBLISH-TOPOLOGY", "root-convention-projection")]
    public void RootConvention_IsAddedOnlyWhenItProjectsToTheMessageContract()
    {
        var trace = new List<string>();
        var topology = new MessagePublishTopology<Message>(new PublishTopology());

        Assert.False(topology.TryAddConvention(new MissingRootConvention()));
        Assert.True(topology.TryAddConvention(new RecordingRootConvention(trace)));

        topology.Apply(new RecordingBuilder<PublishContext<Message>>(trace));

        Assert.Equal(["create-delegated", "root-Message:direct"], trace);
        Assert.False(topology.TryGetPublishAddress(new Uri("loopback://localhost"), out Uri? address));
        Assert.Null(address);
        Assert.Empty(topology.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-PUBLISH-TOPOLOGY", "explicit-attribute-and-fault-exclusion")]
    public void Exclude_CombinesExplicitAttributeFaultAndOverrideRules()
    {
        var publishTopology = new PublishTopology();

        var attributed = new MessagePublishTopology<ExcludedMessage>(publishTopology);
        Assert.True(attributed.Exclude);

        IMessagePublishTopologyConfigurator<Message> source =
            ((IPublishTopologyConfigurator)publishTopology).GetMessageTopology<Message>();
        source.Exclude = true;
        var fault = new MessagePublishTopology<Fault<Message>>(publishTopology);
        Assert.True(fault.Exclude);

        var regular = new MessagePublishTopology<SecondMessage>(publishTopology);
        Assert.False(regular.Exclude);
        regular.Exclude = true;
        Assert.True(regular.Exclude);
        regular.Exclude = false;
        Assert.False(regular.Exclude);
    }

    private sealed record Message;

    private sealed record SecondMessage;

    [ExcludeFromTopology]
    private sealed record ExcludedMessage;

    private sealed class RecordingTopology<TMessage>(List<string> trace, string marker) : IMessagePublishTopology<TMessage>
        where TMessage : class
    {
        public bool Exclude => false;

        public void Apply(ITopologyPipeBuilder<PublishContext<TMessage>> builder)
        {
            trace.Add($"{marker}:{(builder.IsDelegated ? "delegated" : "direct")}");
        }

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            publishAddress = null;
            return false;
        }
    }

    private sealed class RecordingConvention<TMessage>(IMessagePublishTopology<TMessage> topology) :
        IMessagePublishTopologyConvention<TMessage>
        where TMessage : class
    {
        public bool TryGetMessagePublishTopology([NotNullWhen(true)] out IMessagePublishTopology<TMessage>? messagePublishTopology)
        {
            messagePublishTopology = topology;
            return true;
        }

        public bool TryGetMessagePublishTopologyConvention<T>(
            [NotNullWhen(true)] out IMessagePublishTopologyConvention<T>? convention)
            where T : class
        {
            convention = this as IMessagePublishTopologyConvention<T>;
            return convention is not null;
        }
    }

    private sealed class MissingConvention<TMessage> : IMessagePublishTopologyConvention<TMessage>
        where TMessage : class
    {
        public bool TryGetMessagePublishTopology([NotNullWhen(true)] out IMessagePublishTopology<TMessage>? messagePublishTopology)
        {
            messagePublishTopology = null;
            return false;
        }

        public bool TryGetMessagePublishTopologyConvention<T>(
            [NotNullWhen(true)] out IMessagePublishTopologyConvention<T>? convention)
            where T : class
        {
            convention = null;
            return false;
        }
    }

    private sealed class MissingRootConvention : IPublishTopologyConvention
    {
        public bool TryGetMessagePublishTopologyConvention<T>(
            [NotNullWhen(true)] out IMessagePublishTopologyConvention<T>? convention)
            where T : class
        {
            convention = null;
            return false;
        }
    }

    private sealed class RecordingRootConvention(List<string> trace) : IPublishTopologyConvention
    {
        public bool TryGetMessagePublishTopologyConvention<T>(
            [NotNullWhen(true)] out IMessagePublishTopologyConvention<T>? convention)
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
