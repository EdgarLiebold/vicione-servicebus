using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class PublishPipeConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-PIPE-LAYERING", "topology-bus-endpoint-and-message-contracts")]
    public void EndpointSpecification_ComposesTopologyBusAndEndpointLayersExactlyOnce()
    {
        var trace = new List<string>();
        var topology = new PublishTopology();
        ((IPublishTopologyConfigurator)topology).GetMessageTopology<IAlphaContract>()
            .Add(new RecordingTopology<IAlphaContract>(trace, "topology-alpha"));
        ((IPublishTopologyConfigurator)topology).GetMessageTopology<ConcreteMessage>()
            .Add(new RecordingTopology<ConcreteMessage>(trace, "topology-concrete"));

        var bus = new PublishPipeConfiguration(topology);
        bus.Configurator.AddPipeSpecification<IAlphaContract>(
            new RecordingSpecification<PublishContext<IAlphaContract>>(trace, "bus-alpha"));
        bus.Configurator.AddPipeSpecification<ConcreteMessage>(
            new RecordingSpecification<PublishContext<ConcreteMessage>>(trace, "bus-concrete"));

        var endpoint = new PublishPipeConfiguration(bus.Specification);
        endpoint.Configurator.AddPipeSpecification<IAlphaContract>(
            new RecordingSpecification<PublishContext<IAlphaContract>>(trace, "endpoint-alpha"));
        endpoint.Configurator.AddPipeSpecification<ConcreteMessage>(
            new RecordingSpecification<PublishContext<ConcreteMessage>>(trace, "endpoint-concrete"));

        endpoint.Specification.GetMessageSpecification<ConcreteMessage>()
            .Apply(new RecordingBuilder<PublishContext<ConcreteMessage>>());

        Assert.Equal(
        [
            "topology-alpha",
            "bus-alpha",
            "endpoint-alpha",
            "topology-concrete",
            "bus-concrete",
            "endpoint-concrete",
        ], trace);
    }

    private interface IAlphaContract
    {
    }

    private sealed class ConcreteMessage : IAlphaContract
    {
    }

    private sealed class RecordingTopology<TMessage>(List<string> trace, string marker) : IMessagePublishTopology<TMessage>
        where TMessage : class
    {
        public bool Exclude => false;

        public void Apply(ITopologyPipeBuilder<PublishContext<TMessage>> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            trace.Add(marker);
        }

        public bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
        {
            publishAddress = null;
            return false;
        }
    }

    private sealed class RecordingSpecification<TContext>(List<string> trace, string marker) : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public void Apply(IPipeBuilder<TContext> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            trace.Add(marker);
        }

        public IEnumerable<ValidationResult> Validate() => [];
    }

    private sealed class RecordingBuilder<TContext> : ISpecificationPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        private readonly bool _isDelegated;
        private readonly bool _isImplemented;

        public RecordingBuilder(bool isDelegated = false, bool isImplemented = false)
        {
            _isDelegated = isDelegated;
            _isImplemented = isImplemented;
        }

        public bool IsDelegated => _isDelegated;

        public bool IsImplemented => _isImplemented;

        public void AddFilter(IFilter<TContext> filter) => ArgumentNullException.ThrowIfNull(filter);

        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder() =>
            new RecordingBuilder<TContext>(isDelegated: true, isImplemented: _isImplemented);

        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder() =>
            new RecordingBuilder<TContext>(isDelegated: _isDelegated, isImplemented: true);
    }
}
