using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class RabbitMqConfiguredPipeValidationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CONFIGURED-PIPE-VALIDATION", "channel-and-connection-specification-failures-reach-endpoint-validation")]
    public void ConfiguredPipeSpecification_ReportsItsFailureBeforeEndpointBuild(bool connectionPipe)
    {
        var configuration = new RabbitMqBusConfiguration(
            new RabbitMqTopologyConfiguration(RabbitMqBusFactory.CreateMessageTopology()));
        var endpoint = Assert.IsType<RabbitMqReceiveEndpointConfiguration>(
            configuration.HostConfiguration.CreateReceiveEndpointConfiguration("validation-orders", (Action<IRabbitMqReceiveEndpointConfigurator>?)null));
        Assert.DoesNotContain(endpoint.Validate(), result => result.Disposition == ValidationResultDisposition.Failure);

        var failure = new MarkerFailure(connectionPipe ? "ConnectionSpecification" : "ChannelSpecification");
        var channel = new InvalidSpecification<ChannelContext>(failure);
        var connection = new InvalidSpecification<ConnectionContext>(failure);
        int callbackCalls = 0;
        if (connectionPipe)
            endpoint.ConfigureConnection(pipe =>
            {
                callbackCalls++;
                pipe.AddPipeSpecification(connection);
            });
        else
            endpoint.ConfigureChannel(pipe =>
            {
                callbackCalls++;
                pipe.AddPipeSpecification(channel);
            });
        Assert.Equal(1, callbackCalls);
        Assert.Equal(0, channel.ValidateCalls + connection.ValidateCalls);
        Assert.Equal(0, channel.ApplyCalls + connection.ApplyCalls);

        ValidationResult[] results = endpoint.Validate().ToArray();
        Assert.Equal(1, connectionPipe ? connection.ValidateCalls : channel.ValidateCalls);
        Assert.Equal(0, connectionPipe ? channel.ValidateCalls : connection.ValidateCalls);
        ValidationResult reported = Assert.Single(results, result => result.Message == failure.Message);
        Assert.Equal(ValidationResultDisposition.Failure, reported.Disposition);
        Assert.Equal("validation-orders." + failure.Key, reported.Key);
        Assert.Equal(failure.Value, reported.Value);
        Assert.Equal(0, channel.ApplyCalls + connection.ApplyCalls);
    }

    sealed class InvalidSpecification<TContext>(ValidationResult failure) : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public int ValidateCalls;
        public int ApplyCalls;

        public IEnumerable<ValidationResult> Validate()
        {
            ValidateCalls++;
            return new[] { failure };
        }

        public void Apply(IPipeBuilder<TContext> builder)
        {
            ApplyCalls++;
        }
    }

    sealed class MarkerFailure(string key) : ValidationResult
    {
        public ValidationResultDisposition Disposition => ValidationResultDisposition.Failure;
        public string Message => "Controlled invalid configured pipe: " + Key;
        public string Key { get; } = key;
        public string? Value => "controlled-invalid-value";
    }
}
