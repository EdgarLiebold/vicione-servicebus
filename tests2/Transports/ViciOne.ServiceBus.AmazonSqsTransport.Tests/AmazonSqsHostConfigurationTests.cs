using System.Reflection;
using global::Amazon;
using global::Amazon.Runtime;
using global::Amazon.SimpleNotificationService;
using global::Amazon.SQS;
using ViciOne.ServiceBus.AmazonSqsTransport.Configuration;
using ViciOne.ServiceBus.AmazonSqsTransport.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Tests;

public sealed class AmazonSqsHostConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-CREDENTIALS", "secret-free-public-api")]
    public void PublicConfiguration_ContainsNoRawCredentialOrProductTestHarnessSurface()
    {
        Assembly assembly = typeof(IAmazonSqsHostConfigurator).Assembly;
        Type[] publicTypes =
        [
            typeof(IAmazonSqsHostConfigurator),
            typeof(AmazonSqsHostSettings),
            typeof(AmazonSqsTransportOptions),
            typeof(AmazonSqsBusFactoryConfiguratorExtensions),
        ];

        Assert.All(
            publicTypes,
            type => Assert.DoesNotContain(
                type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
                member => member.Name is "AccessKey" or "SecretKey" or "LocalstackHost"));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Testing.AmazonSqsTestHarness"));
        Assert.DoesNotContain(typeof(IAmazonSqsHostConfigurator).GetMethods(), method => method.Name == "Config");
        Assert.Contains(typeof(IAmazonSqsHostConfigurator).GetMethods(), method => method.Name == "ClientFactories");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-CREDENTIALS", "sdk-chain-or-explicit-credential-object")]
    public void HostComposition_UsesTheSdkChainOrAnExplicitCredentialObject()
    {
        AmazonSqsBusConfiguration defaultBus = CreateBusConfiguration();
        var defaultConfigurator = new AmazonSqsBusFactoryConfigurator(defaultBus);

        defaultConfigurator.UseDefaultHost(RegionEndpoint.EUCentral1);

        var defaultSettings = Assert.IsType<ConfigurationHostSettings>(defaultBus.HostConfiguration.Settings);
        Assert.Null(defaultSettings.Credentials);
        Assert.Equal(RegionEndpoint.EUCentral1, defaultSettings.Region);

        AmazonSqsBusConfiguration explicitBus = CreateBusConfiguration();
        var explicitConfigurator = new AmazonSqsBusFactoryConfigurator(explicitBus);
        var credentials = new AnonymousAWSCredentials();
        explicitConfigurator.Host(new Uri("amazonsqs://eu-central-1"), host => host.Credentials(credentials));

        var explicitSettings = Assert.IsType<ConfigurationHostSettings>(explicitBus.HostConfiguration.Settings);
        Assert.Same(credentials, explicitSettings.Credentials);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-CREDENTIALS", "uri-credentials-rejected")]
    public void HostUris_RejectEmbeddedCredentials()
    {
        AmazonSqsTransportConfigurationException exception = Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => new AmazonSqsHostConfigurator(new Uri("amazonsqs://access:secret@eu-central-1")));

        Assert.Contains("must not be embedded", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "immutable-host-snapshot-and-client-factories")]
    public void HostConfiguration_IsFrozenAndRejectsAmbiguousOrNullClientSources()
    {
        var configurator = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1"));

        Assert.Throws<ArgumentNullException>(() => configurator.Credentials(null!));
        Assert.Throws<ArgumentNullException>(() => configurator.ClientFactories(null!, () => CreateSnsClient()));
        Assert.Throws<ArgumentNullException>(() => configurator.ClientFactories(() => CreateSqsClient(), null!));

        var credentials = new AnonymousAWSCredentials();
        configurator.Credentials(credentials);
        Assert.Throws<InvalidOperationException>(() => configurator.ClientFactories(() => CreateSqsClient(), () => CreateSnsClient()));

        AmazonSqsHostSettings frozen = configurator.Settings;
        Assert.Throws<InvalidOperationException>(() => configurator.Scope("changed-after-freeze", false));
        Assert.Equal(new Uri("amazonsqs://eu-central-1/"), frozen.HostAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "fresh-owned-client-pair-per-reconnect")]
    public void ClientFactories_CreateAFreshOwnedPairForEveryConnection()
    {
        var configurator = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1"));
        var sqsCreated = 0;
        var snsCreated = 0;
        var sqsDisposed = 0;
        var snsDisposed = 0;
        configurator.ClientFactories(
            () =>
            {
                Interlocked.Increment(ref sqsCreated);
                return CreateSqsClient(() => Interlocked.Increment(ref sqsDisposed));
            },
            () =>
            {
                Interlocked.Increment(ref snsCreated);
                return CreateSnsClient(() => Interlocked.Increment(ref snsDisposed));
            });

        AmazonSqsHostSettings settings = configurator.Settings;
        IConnection first = settings.CreateConnection();
        IConnection second = settings.CreateConnection();

        Assert.NotSame(first.SqsClient, second.SqsClient);
        Assert.NotSame(first.SnsClient, second.SnsClient);
        Assert.Equal(2, Volatile.Read(ref sqsCreated));
        Assert.Equal(2, Volatile.Read(ref snsCreated));

        first.Dispose();
        second.Dispose();
        Assert.Equal(2, Volatile.Read(ref sqsDisposed));
        Assert.Equal(2, Volatile.Read(ref snsDisposed));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "host-equality-and-hash-contract")]
    public void HostSettingsEquality_UsesTheSameIdentityForEqualityAndHashing()
    {
        var first = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1, Scope = "production" };
        var same = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1, Scope = "production" };
        var differentScope = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1, Scope = "staging" };

        Assert.True(AmazonSqsHostEqualityComparer.Default.Equals(first, same));
        Assert.Equal(
            AmazonSqsHostEqualityComparer.Default.GetHashCode(first),
            AmazonSqsHostEqualityComparer.Default.GetHashCode(same));
        Assert.False(AmazonSqsHostEqualityComparer.Default.Equals(first, differentScope));
    }

    private static AmazonSqsBusConfiguration CreateBusConfiguration() =>
        new(new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology()));

    private static IAmazonSQS CreateSqsClient(Action? dispose = null) =>
        InterfaceProxy<IAmazonSQS>.Create((method, _) => method.Name == nameof(IDisposable.Dispose)
            ? Invoke(dispose)
            : throw new NotSupportedException(method.Name));

    private static IAmazonSimpleNotificationService CreateSnsClient(Action? dispose = null) =>
        InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) => method.Name == nameof(IDisposable.Dispose)
            ? Invoke(dispose)
            : throw new NotSupportedException(method.Name));

    private static object? Invoke(Action? action)
    {
        action?.Invoke();
        return null;
    }
}
