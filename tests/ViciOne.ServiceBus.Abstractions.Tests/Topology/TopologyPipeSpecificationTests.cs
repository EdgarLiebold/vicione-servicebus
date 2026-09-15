using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class TopologyPipeSpecificationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "consume-filter-and-child-state-adaptation")]
    public void ConsumeSpecification_ForwardsFiltersAndPreservesEveryChildState()
    {
        var topology = new ExercisingConsumeTopology();
        var specification = new MessageConsumeTopologyPipeSpecification<Message>(topology);
        var builder = new CapturingSpecificationBuilder<ConsumeContext<Message>>(isImplemented: true);

        specification.Apply(builder);

        AssertTopologyState(topology.InitialBuilder, topology.ChildBuilder, topology.GrandchildBuilder);
        Assert.Equal(3, builder.Filters.Count);
        Assert.Empty(specification.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "publish-filter-and-child-state-adaptation")]
    public void PublishSpecification_ForwardsFiltersAndPreservesEveryChildState()
    {
        var topology = new ExercisingPublishTopology();
        var specification = new MessagePublishTopologyPipeSpecification<Message>(topology);
        var builder = new CapturingSpecificationBuilder<PublishContext<Message>>(isImplemented: true);

        specification.Apply(builder);

        AssertTopologyState(topology.InitialBuilder, topology.ChildBuilder, topology.GrandchildBuilder);
        Assert.Equal(3, builder.Filters.Count);
        Assert.Empty(specification.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "send-filter-and-child-state-adaptation")]
    public void SendSpecification_ForwardsFiltersAndPreservesEveryChildState()
    {
        var topology = new ExercisingSendTopology();
        var specification = new MessageSendTopologyPipeSpecification<Message>(topology);
        var builder = new CapturingSpecificationBuilder<SendContext<Message>>(isImplemented: true);

        specification.Apply(builder);

        AssertTopologyState(topology.InitialBuilder, topology.ChildBuilder, topology.GrandchildBuilder);
        Assert.Equal(3, builder.Filters.Count);
        Assert.Empty(specification.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "adapted-builders-require-filters")]
    public void AdaptedBuilders_RejectMissingFiltersForEveryContextShape()
    {
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
            new MessageConsumeTopologyPipeSpecification<Message>(new NullConsumeFilterTopology())
                .Apply(new CapturingSpecificationBuilder<ConsumeContext<Message>>())).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
            new MessagePublishTopologyPipeSpecification<Message>(new NullPublishFilterTopology())
                .Apply(new CapturingSpecificationBuilder<PublishContext<Message>>())).ParamName);
        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(() =>
            new MessageSendTopologyPipeSpecification<Message>(new NullSendFilterTopology())
                .Apply(new CapturingSpecificationBuilder<SendContext<Message>>())).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "closed-message-topology-pipe-adapters")]
    public void ConcretePipeAdaptersAndTheirImplementationTypes_AreClosedForInheritance()
    {
        Type[] roots =
        [
            typeof(MessageConsumeTopologyPipeSpecification<>),
            typeof(MessagePublishTopologyPipeSpecification<>),
            typeof(MessageSendTopologyPipeSpecification<>)
        ];

        foreach (Type root in roots)
        {
            Assert.True(root.IsSealed, root.FullName);
            Assert.All(Descendants(root).Where(type => type.IsClass), type => Assert.True(type.IsSealed, type.FullName));
        }
    }

    private static void AssertTopologyState<TContext>(
        ITopologyPipeBuilder<TContext>? initial,
        ITopologyPipeBuilder<TContext>? child,
        ITopologyPipeBuilder<TContext>? grandchild)
        where TContext : class, PipeContext
    {
        Assert.NotNull(initial);
        Assert.False(initial.IsDelegated);
        Assert.True(initial.IsImplemented);
        Assert.NotNull(child);
        Assert.True(child.IsDelegated);
        Assert.True(child.IsImplemented);
        Assert.NotNull(grandchild);
        Assert.True(grandchild.IsDelegated);
        Assert.True(grandchild.IsImplemented);
    }

    private static IEnumerable<Type> Descendants(Type type)
    {
        foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
        {
            yield return nestedType;

            foreach (Type descendant in Descendants(nestedType))
                yield return descendant;
        }
    }

    private sealed record Message;

    private sealed class ExercisingConsumeTopology : IMessageConsumeTopology<Message>
    {
        internal ITopologyPipeBuilder<ConsumeContext<Message>>? InitialBuilder { get; private set; }

        internal ITopologyPipeBuilder<ConsumeContext<Message>>? ChildBuilder { get; private set; }

        internal ITopologyPipeBuilder<ConsumeContext<Message>>? GrandchildBuilder { get; private set; }

        public void Apply(ITopologyPipeBuilder<ConsumeContext<Message>> builder)
        {
            InitialBuilder = builder;
            builder.AddFilter(new NoOpFilter<ConsumeContext<Message>>());
            ChildBuilder = builder.CreateDelegatedBuilder();
            ChildBuilder.AddFilter(new NoOpFilter<ConsumeContext<Message>>());
            GrandchildBuilder = ChildBuilder.CreateDelegatedBuilder();
            GrandchildBuilder.AddFilter(new NoOpFilter<ConsumeContext<Message>>());
        }
    }

    private sealed class ExercisingPublishTopology : IMessagePublishTopology<Message>
    {
        internal ITopologyPipeBuilder<PublishContext<Message>>? InitialBuilder { get; private set; }

        internal ITopologyPipeBuilder<PublishContext<Message>>? ChildBuilder { get; private set; }

        internal ITopologyPipeBuilder<PublishContext<Message>>? GrandchildBuilder { get; private set; }

        public bool Exclude => false;

        public void Apply(ITopologyPipeBuilder<PublishContext<Message>> builder)
        {
            InitialBuilder = builder;
            builder.AddFilter(new NoOpFilter<PublishContext<Message>>());
            ChildBuilder = builder.CreateDelegatedBuilder();
            ChildBuilder.AddFilter(new NoOpFilter<PublishContext<Message>>());
            GrandchildBuilder = ChildBuilder.CreateDelegatedBuilder();
            GrandchildBuilder.AddFilter(new NoOpFilter<PublishContext<Message>>());
        }

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            publishAddress = null;
            return false;
        }
    }

    private sealed class ExercisingSendTopology : IMessageSendTopology<Message>
    {
        internal ITopologyPipeBuilder<SendContext<Message>>? InitialBuilder { get; private set; }

        internal ITopologyPipeBuilder<SendContext<Message>>? ChildBuilder { get; private set; }

        internal ITopologyPipeBuilder<SendContext<Message>>? GrandchildBuilder { get; private set; }

        public void Apply(ITopologyPipeBuilder<SendContext<Message>> builder)
        {
            InitialBuilder = builder;
            builder.AddFilter(new NoOpFilter<SendContext<Message>>());
            ChildBuilder = builder.CreateDelegatedBuilder();
            ChildBuilder.AddFilter(new NoOpFilter<SendContext<Message>>());
            GrandchildBuilder = ChildBuilder.CreateDelegatedBuilder();
            GrandchildBuilder.AddFilter(new NoOpFilter<SendContext<Message>>());
        }
    }

    private sealed class NullConsumeFilterTopology : IMessageConsumeTopology<Message>
    {
        public void Apply(ITopologyPipeBuilder<ConsumeContext<Message>> builder) => builder.AddFilter(null!);
    }

    private sealed class NullPublishFilterTopology : IMessagePublishTopology<Message>
    {
        public bool Exclude => false;

        public void Apply(ITopologyPipeBuilder<PublishContext<Message>> builder) => builder.AddFilter(null!);

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            publishAddress = null;
            return false;
        }
    }

    private sealed class NullSendFilterTopology : IMessageSendTopology<Message>
    {
        public void Apply(ITopologyPipeBuilder<SendContext<Message>> builder) => builder.AddFilter(null!);
    }

    private sealed class NoOpFilter<TContext> : IFilter<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context, IPipe<TContext> next) => next.SendAsync(context);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class CapturingSpecificationBuilder<TContext> : ISpecificationPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        readonly List<IFilter<TContext>> _filters = new();

        internal CapturingSpecificationBuilder(bool isDelegated = false, bool isImplemented = false)
        {
            IsDelegated = isDelegated;
            IsImplemented = isImplemented;
        }

        internal IReadOnlyList<IFilter<TContext>> Filters => _filters;

        public bool IsDelegated { get; }

        public bool IsImplemented { get; }

        public void AddFilter(IFilter<TContext> filter) => _filters.Add(filter);

        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder() =>
            new CapturingSpecificationBuilder<TContext>(true, IsImplemented);

        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder() =>
            new CapturingSpecificationBuilder<TContext>(IsDelegated, true);
    }
}
