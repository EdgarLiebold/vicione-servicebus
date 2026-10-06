using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsConfiguredPipeValidationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CONFIGURED-PIPE-VALIDATION", "client-and-connection-specification-failures-reach-endpoint-validation")]
    public void ConfiguredPipeSpecification_ReportsItsFailureBeforeEndpointBuild(bool connectionPipe)
    {
        var configuration = new AmazonSqsBusConfiguration(
            new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology()));
        configuration.HostConfiguration.Settings = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1/")).Settings;
        var endpoint = Assert.IsType<AmazonSqsReceiveEndpointConfiguration>(
            configuration.HostConfiguration.CreateReceiveEndpointConfiguration("validation-orders", (Action<IAmazonSqsReceiveEndpointConfigurator>?)null));
        Assert.DoesNotContain(endpoint.Validate(), result => result.Disposition == ValidationResultDisposition.Failure);

        var failure = new MarkerFailure(connectionPipe ? "ConnectionSpecification" : "ClientSpecification");
        var client = new InvalidSpecification<ClientContext>(failure);
        var connection = new InvalidSpecification<ConnectionContext>(failure);
        int callbackCalls = 0;
        if (connectionPipe)
            endpoint.ConfigureConnection(pipe =>
            {
                callbackCalls++;
                pipe.AddPipeSpecification(connection);
            });
        else
            endpoint.ConfigureClient(pipe =>
            {
                callbackCalls++;
                pipe.AddPipeSpecification(client);
            });
        Assert.Equal(1, callbackCalls);
        Assert.Equal(0, client.ValidateCalls + connection.ValidateCalls);
        Assert.Equal(0, client.ApplyCalls + connection.ApplyCalls);

        ValidationResult[] results = endpoint.Validate().ToArray();
        Assert.Equal(1, connectionPipe ? connection.ValidateCalls : client.ValidateCalls);
        Assert.Equal(0, connectionPipe ? client.ValidateCalls : connection.ValidateCalls);
        ValidationResult reported = Assert.Single(results, result => result.Message == failure.Message);
        Assert.Equal(ValidationResultDisposition.Failure, reported.Disposition);
        Assert.EndsWith(failure.Key, reported.Key, StringComparison.Ordinal);
        Assert.Equal(failure.Value, reported.Value);
        Assert.Equal(0, client.ApplyCalls + connection.ApplyCalls);
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
