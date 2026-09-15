using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Topology;

public sealed class TopologyBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CORRELATION", "required-delegates-and-messages")]
    public void CorrelationResolvers_RejectEveryMissingRequiredInput()
    {
        Assert.Equal("getCorrelationId", Assert.Throws<ArgumentNullException>(
            () => new DelegateMessageCorrelationId<Message>(null!)).ParamName);
        Assert.Equal("getCorrelationId", Assert.Throws<ArgumentNullException>(
            () => new NullableDelegateMessageCorrelationId<Message>(null!)).ParamName);

        var required = new DelegateMessageCorrelationId<Message>(message => message.CorrelationId);
        var nullable = new NullableDelegateMessageCorrelationId<Message>(message => message.CorrelationId);

        Assert.Equal("message", Assert.Throws<ArgumentNullException>(
            () => required.TryGetCorrelationId(null!, out _)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(
            () => nullable.TryGetCorrelationId(null!, out _)).ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CORRELATION", "required-property-name")]
    public void PropertySelector_RejectsAMissingPropertyName(string? propertyName)
    {
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(
            () => new PropertyCorrelationIdSelector<Message>(propertyName!));

        Assert.Equal("propertyName", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CORRELATION", "required-fixed-selector")]
    public void FixedSelector_RejectsAMissingCorrelationResolver()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new SetCorrelationIdSelector<Message>(null!));

        Assert.Equal("messageCorrelationId", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-COLLECTION", "required-comparers")]
    public void EntityCollections_RejectMissingIdentityComparers()
    {
        Assert.Equal("entityComparer", Assert.Throws<ArgumentNullException>(
            () => new EntityCollection<TestEntity, EntityHandle>(null!)).ParamName);
        Assert.Equal("entityComparer", Assert.Throws<ArgumentNullException>(
            () => new NamedEntityCollection<TestEntity, EntityHandle>(null!, TestEntityNameComparer.Instance)).ParamName);
        Assert.Equal("nameComparer", Assert.Throws<ArgumentNullException>(
            () => new NamedEntityCollection<TestEntity, EntityHandle>(TestEntityComparer.Instance, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-ENTITY-COLLECTION", "identity-conflicts-and-lookups")]
    public void EntityCollection_ReportsTheOwningArgumentForIdentityConflictsAndLookups()
    {
        var collection = new EntityCollection<TestEntity, EntityHandle>(TestEntityComparer.Instance);
        var existing = new TestEntity(1, "orders", "durable");
        collection.GetOrAdd(existing);

        Assert.Same(existing, collection.GetOrAdd(new TestEntity(2, "orders", "durable")));
        Assert.Equal([existing], collection.ToArray());
        Assert.Equal([existing], ((System.Collections.IEnumerable)collection).Cast<TestEntity>().ToArray());
        Assert.Equal("entity", Assert.Throws<ArgumentException>(
            () => collection.GetOrAdd(new TestEntity(1, "events", "temporary"))).ParamName);
        Assert.Equal("entityHandle", Assert.Throws<ArgumentException>(
            () => collection.Get(new TestHandle(9))).ParamName);
        Assert.Equal("entityHandle", Assert.Throws<ArgumentException>(
            () => collection.Get(new TestHandle(1))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONSUME", "required-runtime-collaborators")]
    public void ConsumeTopology_RejectsMissingRuntimeCollaboratorsBeforeStateAccess()
    {
        var topology = new TestConsumeTopology();

        Assert.Equal("messageType", Assert.Throws<ArgumentNullException>(
            () => topology.GetMessageTopology(null!)).ParamName);
        Assert.Equal("messageType", Assert.Throws<ArgumentException>(
            () => topology.GetMessageTopology(typeof(int))).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(
            () => topology.TryAddConvention(null!)).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(
            () => topology.ConnectConsumeTopologyConfigurationObserver(null!)).ParamName);
        Assert.Equal("callback", Assert.Throws<ArgumentNullException>(
            () => topology.InvokeAll(null!)).ParamName);
        Assert.Equal("selector", Assert.Throws<ArgumentNullException>(
            () => topology.InvokeSelectMany(null!)).ParamName);
        Assert.Equal("callback", Assert.Throws<ArgumentNullException>(
            () => topology.InvokeForEach(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-OBSERVATION", "consume-owner-notification")]
    public void ConsumeTopology_NotifiesObserversWithTheExactCreatedConfigurator()
    {
        var topology = new TestConsumeTopology();
        var observer = new CapturingConsumeTopologyObserver();
        topology.ConnectConsumeTopologyConfigurationObserver(observer);

        IMessageConsumeTopologyConfigurator configuration = topology.GetMessageTopology(typeof(Message));

        Assert.Same(configuration, observer.Configuration);
        Assert.Equal(typeof(Message), observer.MessageType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONSUME-TOPOLOGY", "required-configuration-inputs")]
    public void MessageConsumeTopology_RejectsEveryMissingConfigurationInput()
    {
        var topology = new MessageConsumeTopology<Message>();

        Assert.Equal("consumeTopology", Assert.Throws<ArgumentNullException>(() => topology.Add(null!)).ParamName);
        Assert.Equal("configuration", Assert.Throws<ArgumentNullException>(() => topology.AddDelegate(null!)).ParamName);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() => topology.Apply(null!)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(
            () => topology.TryAddConvention((IMessageConsumeTopologyConvention<Message>)null!)).ParamName);
        Assert.Equal("convention", Assert.Throws<ArgumentNullException>(
            () => topology.TryAddConvention((IConsumeTopologyConvention)null!)).ParamName);
        Assert.Equal("update", Assert.Throws<ArgumentNullException>(
            () => topology.UpdateConvention<TestConsumeConvention>(null!)).ParamName);
        Assert.Equal("add", Assert.Throws<ArgumentNullException>(
            () => topology.AddOrUpdateConvention<TestConsumeConvention>(null!, convention => convention)).ParamName);
        Assert.Equal("update", Assert.Throws<ArgumentNullException>(
            () => topology.AddOrUpdateConvention(() => new TestConsumeConvention(), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TOPOLOGY-CONVENTION", "required-formatter-inputs")]
    public void SendTopologyConventions_RejectEveryMissingFormatter()
    {
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(
            () => new PartitionKeyMessageSendTopologyConvention<Message>((IPartitionKeyFormatter)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(
            () => new RoutingKeyMessageSendTopologyConvention<Message>((IRoutingKeyFormatter)null!)).ParamName);

        var partition = new PartitionKeyMessageSendTopologyConvention<Message>();
        var routing = new RoutingKeyMessageSendTopologyConvention<Message>();
        var serializer = new SetSerializerMessageSendTopologyConvention<Message>();
        var correlation = new CorrelationIdMessageSendTopologyConvention<Message>();

        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(
            () => partition.SetFormatter((IPartitionKeyFormatter)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(
            () => partition.SetFormatter((IMessagePartitionKeyFormatter<Message>)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(
            () => routing.SetFormatter((IRoutingKeyFormatter)null!)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(
            () => routing.SetFormatter((IMessageRoutingKeyFormatter<Message>)null!)).ParamName);
        Assert.Equal("contentType", Assert.Throws<ArgumentNullException>(
            () => serializer.SetSerializer(null!)).ParamName);
        Assert.Equal("messageCorrelationId", Assert.Throws<ArgumentNullException>(
            () => correlation.SetCorrelationId(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CORRELATION", "explicit-resolver-precedence")]
    public void CorrelationConvention_PrefersAnExplicitResolverOverInferredSources()
    {
        var convention = new CorrelationIdMessageSendTopologyConvention<Message>();
        var explicitResolver = new DelegateMessageCorrelationId<Message>(_ => Guid.Parse("aeb0f42b-faea-4f47-aa2f-b07d0f6033b2"));

        Assert.True(convention.TryGetCorrelationIdResolver(out IMessageCorrelationId<Message>? inferredResolver));
        Assert.NotSame(explicitResolver, inferredResolver);

        convention.SetCorrelationId(explicitResolver);

        Assert.True(convention.TryGetCorrelationIdResolver(out IMessageCorrelationId<Message>? selectedResolver));
        Assert.Same(explicitResolver, selectedResolver);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PUBLIC-API", "implementation-types-remain-internal")]
    public void TopologyImplementationTypes_AreSealedAndAbsentFromThePublicApi()
    {
        Type[] implementationTypes =
        [
            typeof(CorrelatedByCorrelationIdSelector<>),
            typeof(CorrelationIdMessageSendTopologyConvention<>),
            typeof(PropertyCorrelationIdSelector<>),
            typeof(SetCorrelationIdSelector<>),
            typeof(PartitionKeyMessageSendTopologyConvention<>),
            typeof(RoutingKeyMessageSendTopologyConvention<>),
            typeof(SetSerializerMessageSendTopologyConvention<>),
            typeof(SetCorrelationIdMessageSendTopology<>),
            typeof(SetPartitionKeyMessageSendTopology<>),
            typeof(SetRoutingKeyMessageSendTopology<>),
            typeof(SetSerializerMessageSendTopology<>)
        ];

        Assert.All(implementationTypes, type =>
        {
            Assert.False(type.IsPublic);
            Assert.True(type.IsSealed);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PUBLIC-API", "closed-root-correlation-convention")]
    public void RootCorrelationConvention_IsClosedForInheritance()
    {
        Assert.True(typeof(CorrelationIdSendTopologyConvention).IsSealed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONVENTION-CACHE", "minimal-greenfield-surface")]
    public void ConventionCache_ExposesOnlyItsRequiredFactoryAndNoEmptyMarkerContract()
    {
        var constructor = Assert.Single(typeof(TopologyConventionCache<>).GetConstructors());
        ParameterInfo parameter = Assert.Single(constructor.GetParameters());

        Assert.True(typeof(TopologyConventionCache<>).IsSealed);
        Assert.Equal("typeFactory", parameter.Name);
        Assert.Equal(typeof(IConventionTypeFactory<>), parameter.ParameterType.GetGenericTypeDefinition());
        Assert.Null(typeof(IMessageTopology).Assembly.GetType(
            "ViciOne.ServiceBus.Configuration.IMessageTypeTopologyConfigurator"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-CONVENTION-CACHE", "one-instance-per-message-contract")]
    public async Task ConventionCache_CreatesOneStableInstancePerMessageContractAsync()
    {
        var factory = new CountingConventionFactory();
        ITopologyConventionCache<TestConventionValue> cache = new TopologyConventionCache<TestConventionValue>(factory);

        TestConventionValue[] values = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() => cache.GetOrAdd<Message, TestConventionValue>())));

        Assert.All(values, value => Assert.Same(values[0], value));
        Assert.Equal(1, factory.Count);
        Assert.NotSame(values[0], cache.GetOrAdd<TestEntity, TestConventionValue>());
        Assert.Equal(2, factory.Count);
    }

    private sealed record Message(Guid CorrelationId);

    private sealed record TestEntity(long Id, string Name, string Settings) : EntityHandle;

    private sealed record TestHandle(long Id) : EntityHandle;

    private sealed class TestEntityComparer : IEqualityComparer<TestEntity>
    {
        internal static readonly TestEntityComparer Instance = new();

        public bool Equals(TestEntity? x, TestEntity? y) =>
            StringComparer.Ordinal.Equals(x?.Name, y?.Name)
            && StringComparer.Ordinal.Equals(x?.Settings, y?.Settings);

        public int GetHashCode(TestEntity obj) => HashCode.Combine(obj.Name, obj.Settings);
    }

    private sealed class TestEntityNameComparer : IEqualityComparer<TestEntity>
    {
        internal static readonly TestEntityNameComparer Instance = new();

        public bool Equals(TestEntity? x, TestEntity? y) => StringComparer.Ordinal.Equals(x?.Name, y?.Name);

        public int GetHashCode(TestEntity obj) => StringComparer.Ordinal.GetHashCode(obj.Name);
    }

    private sealed class TestConsumeConvention : IMessageConsumeTopologyConvention<Message>
    {
        public bool TryGetMessageConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<Message>? messageConsumeTopology)
        {
            messageConsumeTopology = null;
            return false;
        }

        public bool TryGetMessageConsumeTopologyConvention<T>([NotNullWhen(true)] out IMessageConsumeTopologyConvention<T>? convention)
            where T : class
        {
            convention = null;
            return false;
        }
    }

    private sealed class TestConventionValue;

    private sealed class CountingConventionFactory : IConventionTypeFactory<TestConventionValue>
    {
        int _count;

        internal int Count => Volatile.Read(ref _count);

        public TestConventionValue Create<T>()
            where T : class
        {
            Interlocked.Increment(ref _count);
            return new TestConventionValue();
        }
    }

    private sealed class CapturingConsumeTopologyObserver : IConsumeTopologyConfigurationObserver
    {
        internal object? Configuration { get; private set; }

        internal Type? MessageType { get; private set; }

        public void MessageTopologyCreated<T>(IMessageConsumeTopologyConfigurator<T> configuration)
            where T : class
        {
            Configuration = configuration;
            MessageType = typeof(T);
        }
    }

    private sealed class TestConsumeTopology : ConsumeTopology
    {
        internal bool InvokeAll(Func<IMessageConsumeTopologyConfigurator, bool> callback) => All(callback);

        internal IEnumerable<int> InvokeSelectMany(Func<IMessageConsumeTopologyConfigurator, IEnumerable<int>> selector) =>
            SelectMany(selector);

        internal void InvokeForEach(Action<IMessageConsumeTopologyConfigurator> callback) => ForEach(callback);
    }
}
