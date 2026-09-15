using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Topology;

public sealed class PublishToSendTopologyConfigurationObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "send-to-publish-filter-and-state-projection")]
    public void Projection_AdaptsFiltersAndPreservesDelegatedAndImplementedState()
    {
        var send = new SendTopology();
        var sendMessageTopology = new CapturingSendTopology();
        ((ISendTopologyConfigurator)send).AddMessageSendTopology<Message>(sendMessageTopology);
        var observer = new PublishToSendTopologyConfigurationObserver(send);
        var publishMessageTopology = new MessagePublishTopology<Message>(new PublishTopology());
        observer.MessageTopologyCreated(publishMessageTopology);
        var builder = new CapturingPublishBuilder(isImplemented: true);

        publishMessageTopology.Apply(builder);

        Assert.NotNull(sendMessageTopology.Builder);
        Assert.True(sendMessageTopology.Builder.IsDelegated);
        Assert.True(sendMessageTopology.Builder.IsImplemented);
        Assert.NotNull(sendMessageTopology.DelegatedBuilder);
        Assert.True(sendMessageTopology.DelegatedBuilder.IsDelegated);
        Assert.True(sendMessageTopology.DelegatedBuilder.IsImplemented);
        Assert.Equal(3, builder.Filters.Count);
        Assert.All(builder.Filters, filter =>
            Assert.StartsWith("SplitFilter", filter.GetType().Name, StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "send-to-publish-proxy-contract")]
    public void ProjectedTopology_HasNoAddressOrExclusionAndRequiresABaseAddress()
    {
        var observer = new PublishToSendTopologyConfigurationObserver(new SendTopology());
        var configurator = new CapturingPublishConfigurator();

        observer.MessageTopologyCreated(configurator);

        IMessagePublishTopology<Message> projected = Assert.IsAssignableFrom<IMessagePublishTopology<Message>>(
            configurator.DelegatedTopology);
        Assert.False(projected.Exclude);
        Assert.False(projected.TryGetPublishAddress(new Uri("loopback://localhost"), out Uri? publishAddress));
        Assert.Null(publishAddress);
        Assert.Equal("baseAddress", Assert.Throws<ArgumentNullException>(
            () => projected.TryGetPublishAddress(null!, out _)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "send-to-publish-filter-execution")]
    public async Task ProjectedFilter_RoundTripsTheOriginalPublishContextAsync()
    {
        var send = new SendTopology();
        ((ISendTopologyConfigurator)send).AddMessageSendTopology<Message>(new CapturingSendTopology());
        var observer = new PublishToSendTopologyConfigurationObserver(send);
        var publishMessageTopology = new MessagePublishTopology<Message>(new PublishTopology());
        observer.MessageTopologyCreated(publishMessageTopology);
        var builder = new CapturingPublishBuilder();
        publishMessageTopology.Apply(builder);
        IFilter<PublishContext<Message>> splitFilter = builder.Filters[0];
        PublishContext<Message> context = DispatchProxy.Create<PublishContext<Message>, PublishContextProxy>();
        var next = new CapturingPublishPipe();

        await splitFilter.SendAsync(context, next);

        Assert.Same(context, next.Context);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "send-to-publish-required-collaborators")]
    public void Projection_RejectsEveryMissingCollaboratorAtItsOwningBoundary()
    {
        Assert.Equal("sendTopology", Assert.Throws<ArgumentNullException>(
            () => new PublishToSendTopologyConfigurationObserver(null!)).ParamName);

        var observer = new PublishToSendTopologyConfigurationObserver(new SendTopology());
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(
            () => observer.MessageTopologyCreated<Message>(null!)).ParamName);

        var send = new SendTopology();
        ((ISendTopologyConfigurator)send).AddMessageSendTopology<Message>(new NullFilterSendTopology());
        var nullFilterObserver = new PublishToSendTopologyConfigurationObserver(send);
        var publish = new MessagePublishTopology<Message>(new PublishTopology());
        nullFilterObserver.MessageTopologyCreated(publish);

        Assert.Equal("filter", Assert.Throws<ArgumentNullException>(
            () => publish.Apply(new CapturingPublishBuilder())).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TOPOLOGY-PIPE", "closed-send-to-publish-adapter")]
    public void ConcreteProjectionAdapterAndItsImplementationTypes_AreClosedForInheritance()
    {
        Type root = typeof(PublishToSendTopologyConfigurationObserver);
        Assert.True(root.IsSealed);

        Type[] nestedTypes = Descendants(root).Where(type => type.IsClass).ToArray();
        Assert.NotEmpty(nestedTypes);
        Assert.All(nestedTypes, type => Assert.True(type.IsSealed, type.FullName));
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

    private sealed class CapturingSendTopology : IMessageSendTopology<Message>
    {
        internal ITopologyPipeBuilder<SendContext<Message>>? Builder { get; private set; }

        internal ITopologyPipeBuilder<SendContext<Message>>? DelegatedBuilder { get; private set; }

        public void Apply(ITopologyPipeBuilder<SendContext<Message>> builder)
        {
            Builder = builder;
            builder.AddFilter(new NoOpSendFilter());
            DelegatedBuilder = builder.CreateDelegatedBuilder();
            DelegatedBuilder.AddFilter(new NoOpSendFilter());
            DelegatedBuilder.CreateDelegatedBuilder().AddFilter(new NoOpSendFilter());
        }
    }

    private sealed class NullFilterSendTopology : IMessageSendTopology<Message>
    {
        public void Apply(ITopologyPipeBuilder<SendContext<Message>> builder)
        {
            builder.AddFilter(null!);
        }
    }

    private sealed class NoOpSendFilter : IFilter<SendContext<Message>>
    {
        public Task SendAsync(SendContext<Message> context, IPipe<SendContext<Message>> next) => next.SendAsync(context);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class CapturingPublishBuilder : ITopologyPipeBuilder<PublishContext<Message>>
    {
        readonly List<IFilter<PublishContext<Message>>> _filters;

        internal CapturingPublishBuilder(bool isDelegated = false, bool isImplemented = false)
            : this(new List<IFilter<PublishContext<Message>>>(), isDelegated, isImplemented)
        {
        }

        CapturingPublishBuilder(
            List<IFilter<PublishContext<Message>>> filters,
            bool isDelegated,
            bool isImplemented)
        {
            _filters = filters;
            IsDelegated = isDelegated;
            IsImplemented = isImplemented;
        }

        internal IReadOnlyList<IFilter<PublishContext<Message>>> Filters => _filters;

        public bool IsDelegated { get; }

        public bool IsImplemented { get; }

        public void AddFilter(IFilter<PublishContext<Message>> filter)
        {
            _filters.Add(filter);
        }

        public ITopologyPipeBuilder<PublishContext<Message>> CreateDelegatedBuilder() =>
            new CapturingPublishBuilder(_filters, true, IsImplemented);
    }

    private sealed class CapturingPublishConfigurator : IMessagePublishTopologyConfigurator<Message>
    {
        internal IMessagePublishTopology<Message>? DelegatedTopology { get; private set; }

        public bool Exclude { get; set; }

        public void Add(IMessagePublishTopology<Message> publishTopology)
        {
        }

        public void AddDelegate(IMessagePublishTopology<Message> configuration)
        {
            DelegatedTopology = configuration;
        }

        public void Apply(ITopologyPipeBuilder<PublishContext<Message>> builder)
        {
        }

        public void AddOrUpdateConvention<TConvention>(Func<TConvention> add, Func<TConvention, TConvention> update)
            where TConvention : class, IMessagePublishTopologyConvention<Message>
        {
        }

        public bool TryAddConvention(IMessagePublishTopologyConvention<Message> convention) => false;

        public bool TryAddConvention(IPublishTopologyConvention convention) => false;

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            publishAddress = null;
            return false;
        }

        public IEnumerable<ValidationResult> Validate() => [];
    }

    private sealed class CapturingPublishPipe : IPipe<PublishContext<Message>>
    {
        internal PublishContext<Message>? Context { get; private set; }

        public Task SendAsync(PublishContext<Message> context)
        {
            Context = context;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private class PublishContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(PipeContext.TryGetPayload) && args is { Length: 1 })
            {
                Type payloadType = targetMethod.GetGenericArguments()[0];
                if (payloadType.IsInstanceOfType(this))
                {
                    args[0] = this;
                    return true;
                }

                args[0] = null;
                return false;
            }

            Type returnType = targetMethod?.ReturnType ?? typeof(void);
            return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
        }
    }
}
