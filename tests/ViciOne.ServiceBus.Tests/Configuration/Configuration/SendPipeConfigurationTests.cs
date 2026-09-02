using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Topology;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class SendPipeConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-PIPE-LAYERING", "topology-bus-endpoint-and-message-contracts")]
    public void EndpointSpecification_ComposesTopologyBusAndEndpointLayersExactlyOnce()
    {
        var trace = new List<string>();
        var topology = new SendTopology();
        topology.GetMessageTopology<IAlphaContract>().Add(new RecordingTopology<IAlphaContract>(trace, "topology-alpha"));
        topology.GetMessageTopology<ConcreteMessage>().Add(new RecordingTopology<ConcreteMessage>(trace, "topology-concrete"));

        var bus = new SendPipeConfiguration(topology);
        bus.Configurator.AddPipeSpecification<IAlphaContract>(new RecordingSpecification<SendContext<IAlphaContract>>(trace, "bus-alpha"));
        bus.Configurator.AddPipeSpecification<ConcreteMessage>(new RecordingSpecification<SendContext<ConcreteMessage>>(trace, "bus-concrete"));

        var endpoint = new SendPipeConfiguration(bus.Specification);
        endpoint.Configurator.AddPipeSpecification<IAlphaContract>(new RecordingSpecification<SendContext<IAlphaContract>>(trace, "endpoint-alpha"));
        endpoint.Configurator.AddPipeSpecification<ConcreteMessage>(new RecordingSpecification<SendContext<ConcreteMessage>>(trace, "endpoint-concrete"));

        endpoint.Specification.GetMessageSpecification<ConcreteMessage>()
            .Apply(new RecordingBuilder<SendContext<ConcreteMessage>>());

        Assert.Equal(
            [
                "topology-alpha",
                "bus-alpha",
                "endpoint-alpha",
                "topology-concrete",
                "bus-concrete",
                "endpoint-concrete",
            ],
            trace);
    }

    private interface IAlphaContract
    {
    }

    private sealed class ConcreteMessage : IAlphaContract
    {
    }

    private sealed class RecordingTopology<TMessage>(List<string> trace, string marker) : IMessageSendTopology<TMessage>
        where TMessage : class
    {
        public void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            trace.Add(marker);
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
