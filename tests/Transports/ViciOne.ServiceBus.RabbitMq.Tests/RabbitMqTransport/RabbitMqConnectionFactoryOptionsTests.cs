using System.Reflection;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqConnectionFactoryOptionsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(1048576u, null)]
    [InlineData(null, 65536u)]
    [InlineData(2097152u, 131072u)]
    [InlineData(1048576u, 0u)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "optional-message-and-frame-limits-reach-client-independently")]
    public void OptionalLimits_ReachClientIndependentlyAndPreserveUnsetDefaults(uint? messageBytes, uint? frameBytes)
    {
        var host = new RabbitMqHostConfigurator("limits.internal", "/limits", 5678);
        if (messageBytes.HasValue)
            host.MaxMessageSize(messageBytes.Value);
        if (frameBytes.HasValue)
            host.RequestedFrameMax(frameBytes.Value);

        var sdkDefaults = new ConnectionFactory();
        if (messageBytes.HasValue)
            Assert.NotEqual(sdkDefaults.MaxInboundMessageBodySize, messageBytes.Value);
        if (frameBytes is > 0)
            Assert.NotEqual(sdkDefaults.RequestedFrameMax, frameBytes.Value);
        ConnectionFactory client = host.Settings.GetConnectionFactory();

        Assert.Equal(messageBytes ?? sdkDefaults.MaxInboundMessageBodySize, client.MaxInboundMessageBodySize);
        Assert.Equal(frameBytes ?? sdkDefaults.RequestedFrameMax, client.RequestedFrameMax);
        Assert.Equal("limits.internal", client.HostName);
        Assert.Equal(5678, client.Port);
        Assert.Equal("/limits", client.VirtualHost);
        Assert.False(client.AutomaticRecoveryEnabled);
        Assert.False(client.TopologyRecoveryEnabled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-CREDENTIALS", "certificate-identity-precedes-dynamic-provider-and-static-credentials")]
    public void Authentication_SelectsCertificateBeforeProviderAndProviderBeforeStaticCredentials(bool certificateIdentity)
    {
        var credentials = DispatchProxy.Create<ICredentialsProvider, CredentialsProbe>();
        var probe = (CredentialsProbe)(object)credentials;
        var host = new RabbitMqHostConfigurator("auth.internal", "/auth");
        host.Username("static-user-must-not-win");
        host.Password("static-password-must-not-win");
        host.CredentialsProvider = credentials;
        host.UseSsl(ssl => ssl.UseCertificateAsAuthenticationIdentity = certificateIdentity);

        ConnectionFactory client = host.Settings.GetConnectionFactory();

        Assert.True(client.Ssl.Enabled);
        Assert.Equal(0, probe.Calls);
        if (certificateIdentity)
        {
            Assert.NotSame(credentials, client.CredentialsProvider);
            Assert.Equal("", client.UserName);
            Assert.Equal("", client.Password);
            Assert.IsType<ExternalMechanismFactory>(Assert.Single(client.AuthMechanisms));
            Assert.Null(client.AuthMechanismFactory(["PLAIN"]));
        }
        else
        {
            Assert.Same(credentials, client.CredentialsProvider);
            Assert.Equal("guest", client.UserName);
            Assert.Equal("guest", client.Password);
            Assert.IsType<PlainMechanismFactory>(client.AuthMechanismFactory(["PLAIN"]));
            Assert.DoesNotContain(client.AuthMechanisms, mechanism => mechanism is ExternalMechanismFactory);
        }
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "configured-cluster-resolver-overrides-sdk-endpoint-input")]
    public void ClusterResolver_OverridesSdkCandidatesAndRetainsSequentialSelection()
    {
        var host = new RabbitMqHostConfigurator("unused.internal", "/cluster", 5678);
        host.UseSsl(ssl => ssl.ServerName = "cluster-certificate.internal");
        host.UseCluster(cluster =>
        {
            cluster.Node("first.internal:5679");
            cluster.Node("second.internal");
        });
        ConnectionFactory client = host.Settings.GetConnectionFactory();
        var decoy = new AmqpTcpEndpoint("must-not-connect.internal", 5999);

        IEndpointResolver resolver = client.EndpointResolverFactory([decoy]);

        Assert.Equal("", client.HostName);
        Assert.Same(host.Settings.EndpointResolver, resolver);
        Assert.Same(resolver, client.EndpointResolverFactory([]));
        (string Host, int Port)[] expected =
        [
            ("first.internal", 5679),
            ("second.internal", 5678),
            ("first.internal", 5679),
        ];
        foreach ((string hostname, int port) in expected)
        {
            AmqpTcpEndpoint endpoint = Assert.Single(resolver.All());
            Assert.Equal(hostname, endpoint.HostName);
            Assert.Equal(port, endpoint.Port);
            Assert.NotSame(decoy, endpoint);
            Assert.True(endpoint.Ssl.Enabled);
            Assert.Equal("cluster-certificate.internal", endpoint.Ssl.ServerName);
        }
    }

    private class CredentialsProbe : DispatchProxy
    {
        public int Calls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls++;
            throw new InvalidOperationException($"Factory construction must not fetch credentials: {targetMethod?.Name}");
        }
    }
}
