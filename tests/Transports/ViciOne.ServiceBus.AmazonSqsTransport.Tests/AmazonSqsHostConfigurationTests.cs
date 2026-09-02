using System.Reflection;
using System.Runtime.CompilerServices;
using global::Amazon;
using global::Amazon.Runtime;
using global::Amazon.SimpleNotificationService;
using global::Amazon.SimpleNotificationService.Model;
using global::Amazon.SQS;
using global::Amazon.SQS.Model;
using Microsoft.Extensions.Time.Testing;
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
        Assert.Contains(typeof(IAmazonSqsHostConfigurator).GetMethods(), method => method.Name == "ClientContextCache");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-CREDENTIALS", "sdk-chain-or-explicit-credential-object")]
    public void HostComposition_UsesTheSdkChainOrAnExplicitCredentialObject()
    {
        AmazonSqsBusConfiguration defaultBus = CreateBusConfiguration();
        var defaultConfigurator = new AmazonSqsBusFactoryConfigurator(defaultBus);

        defaultConfigurator.UseDefaultHost(RegionEndpoint.EUCentral1);

        AmazonSqsHostSettings defaultSettings = defaultBus.HostConfiguration.Settings;
        Assert.Null(defaultSettings.Credentials);
        Assert.Same(RegionEndpoint.EUCentral1, defaultSettings.Region);

        AmazonSqsBusConfiguration explicitBus = CreateBusConfiguration();
        var explicitConfigurator = new AmazonSqsBusFactoryConfigurator(explicitBus);
        var credentials = new AnonymousAWSCredentials();
        explicitConfigurator.Host(new Uri("amazonsqs://eu-central-1"), host => host.Credentials(credentials));

        AmazonSqsHostSettings explicitSettings = explicitBus.HostConfiguration.Settings;
        Assert.Same(credentials, explicitSettings.Credentials);
    }

    [Theory]
    [InlineData("amazonsqs://access:secret@eu-central-1/", "Credentials")]
    [InlineData("amazonsqs://eu-central-1/?accessKey=secret", "Query")]
    [InlineData("amazonsqs://eu-central-1/#secret", "Fragments")]
    [RequirementCoverage("REQ-VSB-AWS-CREDENTIALS", "uri-credentials-and-control-components-rejected")]
    public void HostUris_RejectCredentialsAndControlComponents(string address, string expectedReason)
    {
        var uri = new Uri(address);

        AmazonSqsTransportConfigurationException addressException = Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => new AmazonSqsHostAddress(uri));
        AmazonSqsTransportConfigurationException configuratorException = Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => new AmazonSqsHostConfigurator(uri));

        Assert.Contains(expectedReason, addressException.Message, StringComparison.Ordinal);
        Assert.Equal(addressException.Message, configuratorException.Message);
    }

    [Theory]
    [InlineData("amazonsqs:/production", UriKind.Absolute, "must be specified")]
    [InlineData("production", UriKind.Relative, "must be absolute")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "host-uri-requires-absolute-address-and-host")]
    public void HostUris_RequireAnAbsoluteAddressWithAHost(string address, UriKind uriKind, string expectedReason)
    {
        var uri = new Uri(address, uriKind);

        AmazonSqsTransportConfigurationException addressException = Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => new AmazonSqsHostAddress(uri));
        AmazonSqsTransportConfigurationException configuratorException = Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => new AmazonSqsHostConfigurator(uri));

        Assert.Contains(expectedReason, addressException.Message, StringComparison.Ordinal);
        Assert.Equal(addressException.Message, configuratorException.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "string-host-rejects-null-empty-or-whitespace")]
    public void StringHostConstruction_RejectsMissingHost(string? host)
    {
        AmazonSqsTransportConfigurationException exception = Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => new AmazonSqsHostAddress(host!, "/"));

        Assert.Contains("must be specified", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "immutable-host-snapshot-and-client-factories")]
    public void HostConfiguration_IsFrozenAndRejectsAmbiguousOrNullClientSources()
    {
        var configurator = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1"));

        Assert.Throws<ArgumentNullException>(() => configurator.Credentials(null!));
        Assert.Throws<ArgumentNullException>(() => configurator.ClientFactories(null!, () => CreateSnsClient()));
        Assert.Throws<ArgumentNullException>(() => configurator.ClientFactories(() => CreateSqsClient(), null!));
        Assert.Throws<ArgumentNullException>(() => configurator.ClientContextCache(null!));

        var credentials = new AnonymousAWSCredentials();
        configurator.Credentials(credentials);
        Assert.Throws<InvalidOperationException>(() => configurator.ClientFactories(() => CreateSqsClient(), () => CreateSnsClient()));

        AmazonSqsHostSettings frozen = configurator.Settings;
        Assert.Throws<InvalidOperationException>(() => configurator.Scope("changed-after-freeze", false));
        Assert.Equal(new Uri("amazonsqs://eu-central-1/"), frozen.HostAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "low-level-host-accepts-only-sealed-immutable-snapshots")]
    public void LowLevelHost_AcceptsOnlyTheSealedSnapshotProducedByTheTypedBuilder()
    {
        Assert.True(typeof(AmazonSqsHostSettings).IsSealed);
        Assert.False(typeof(AmazonSqsHostSettings).IsInterface);
        Assert.Empty(typeof(AmazonSqsHostSettings).GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        var hostConfigurator = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1/production"));
        AmazonSqsHostSettings snapshot = hostConfigurator.Settings;
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();

        new AmazonSqsBusFactoryConfigurator(bus).Host(snapshot);

        Assert.Same(snapshot, bus.HostConfiguration.Settings);
        Assert.Throws<InvalidOperationException>(() => hostConfigurator.Scope("mutated", true));
        Assert.Throws<InvalidOperationException>(() => hostConfigurator.ClientContextCache(new AmazonSqsClientContextCacheOptions()));
        Assert.Equal(new Uri("amazonsqs://eu-central-1/production"), bus.HostConfiguration.Settings.HostAddress);
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

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "owned-client-disposal-attempts-both-clients-and-preserves-failures")]
    public void OwnedClientDisposal_AttemptsBothClientsAndPreservesFailures(bool sqsFails, bool snsFails)
    {
        var sqsFailure = new InvalidOperationException("SQS dispose failed");
        var snsFailure = new InvalidOperationException("SNS dispose failed");
        var sqsDisposed = 0;
        var snsDisposed = 0;
        var configurator = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1"));
        configurator.ClientFactories(
            () => CreateSqsClient(() =>
            {
                sqsDisposed++;
                if (sqsFails)
                    ThrowOnDispose(sqsFailure);
            }),
            () => CreateSnsClient(() =>
            {
                snsDisposed++;
                if (snsFails)
                    ThrowOnDispose(snsFailure);
            }));
        IConnection connection = configurator.Settings.CreateConnection();

        Exception actual = Assert.ThrowsAny<Exception>(connection.Dispose);

        Assert.Equal(1, sqsDisposed);
        Assert.Equal(1, snsDisposed);
        if (sqsFails && snsFails)
        {
            AggregateException aggregate = Assert.IsType<AggregateException>(actual);
            Assert.Equal([snsFailure, sqsFailure], aggregate.InnerExceptions);
        }
        else
        {
            Assert.Same(sqsFails ? sqsFailure : snsFailure, actual);
            Assert.Contains(nameof(ThrowOnDispose), actual.StackTrace, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "partial-client-construction-preserves-primary-and-cleans-sqs")]
    public void PartialClientConstruction_PreservesPrimaryFailureAndAlwaysCleansSqs(bool nullSnsClient, bool cleanupFails)
    {
        var primaryFailure = new InvalidOperationException("SNS creation failed");
        var cleanupFailure = new InvalidOperationException("SQS cleanup failed");
        var cleanupCalls = 0;
        IAmazonSQS sqsClient = CreateSqsClient(() =>
        {
            cleanupCalls++;
            if (cleanupFails)
                ThrowOnDispose(cleanupFailure);
        });

        Exception actual = Assert.ThrowsAny<Exception>(() => new Connection(
            () => sqsClient,
            () => nullSnsClient ? null! : ThrowFromSnsFactory(primaryFailure)));

        Assert.Equal(1, cleanupCalls);
        if (cleanupFails)
        {
            AggregateException aggregate = Assert.IsType<AggregateException>(actual);
            Assert.Equal(2, aggregate.InnerExceptions.Count);
            if (nullSnsClient)
                Assert.Contains("returned null", aggregate.InnerExceptions[0].Message, StringComparison.Ordinal);
            else
                Assert.Same(primaryFailure, aggregate.InnerExceptions[0]);
            Assert.Same(cleanupFailure, aggregate.InnerExceptions[1]);
        }
        else if (nullSnsClient)
            Assert.Contains("returned null", Assert.IsType<InvalidOperationException>(actual).Message, StringComparison.Ordinal);
        else
        {
            Assert.Same(primaryFailure, actual);
            Assert.Contains(nameof(ThrowFromSnsFactory), actual.StackTrace, StringComparison.Ordinal);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "per-host-cache-options-are-immutable-validated-and-time-provider-driven")]
    public async Task ClientContextCacheOptions_ArePerHostAndExpireThroughTheConfiguredTimeProvider()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AmazonSqsClientContextCacheOptions(0, TimeSpan.FromMinutes(1), TimeProvider.System));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AmazonSqsClientContextCacheOptions(8, TimeSpan.Zero, TimeProvider.System));
        Assert.Throws<ArgumentNullException>(
            () => new AmazonSqsClientContextCacheOptions(8, TimeSpan.FromMinutes(1), null!));
        Assert.Null(typeof(AmazonSqsClientContextCacheOptions).GetProperty("MinAge"));

        var shortClock = new FakeTimeProvider(new DateTimeOffset(2042, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var longClock = new FakeTimeProvider(shortClock.GetUtcNow());
        var shortOptions = new AmazonSqsClientContextCacheOptions(8, TimeSpan.FromMinutes(1), shortClock);
        var longOptions = new AmazonSqsClientContextCacheOptions(8, TimeSpan.FromHours(1), longClock);

        (int shortQueueCalls, int shortTopicCalls) = await ExerciseClientContextCaches(shortOptions, shortClock, TimeSpan.FromMinutes(2));
        (int longQueueCalls, int longTopicCalls) = await ExerciseClientContextCaches(longOptions, longClock, TimeSpan.FromMinutes(2));

        Assert.Equal(2, shortQueueCalls);
        Assert.Equal(2, shortTopicCalls);
        Assert.Equal(1, longQueueCalls);
        Assert.Equal(1, longTopicCalls);
        Assert.Equal(8, shortOptions.Capacity);
        Assert.Equal(TimeSpan.FromMinutes(1), shortOptions.MaxAge);
        Assert.Same(shortClock, shortOptions.TimeProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "host-equality-and-hash-contract")]
    public void HostSettingsEquality_UsesTheSameIdentityForEqualityAndHashing()
    {
        AmazonSqsHostSettings first = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1, Scope = "production", ScopeTopics = true }.Freeze();
        AmazonSqsHostSettings same = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1, Scope = "production", ScopeTopics = true }.Freeze();
        AmazonSqsHostSettings differentScope = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1, Scope = "staging", ScopeTopics = true }.Freeze();
        AmazonSqsHostSettings differentScopingMode = new ConfigurationHostSettings { Region = RegionEndpoint.EUCentral1, Scope = "production", ScopeTopics = false }.Freeze();

        Assert.True(AmazonSqsHostEqualityComparer.Default.Equals(first, same));
        Assert.Equal(
            AmazonSqsHostEqualityComparer.Default.GetHashCode(first),
            AmazonSqsHostEqualityComparer.Default.GetHashCode(same));
        Assert.False(AmazonSqsHostEqualityComparer.Default.Equals(first, differentScope));
        Assert.False(AmazonSqsHostEqualityComparer.Default.Equals(first, differentScopingMode));
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

    private static async Task<(int QueueCalls, int TopicCalls)> ExerciseClientContextCaches(
        AmazonSqsClientContextCacheOptions options,
        FakeTimeProvider timeProvider,
        TimeSpan advance)
    {
        var queueCalls = new Dictionary<string, int>(StringComparer.Ordinal);
        var topicCalls = new Dictionary<string, int>(StringComparer.Ordinal);
        IAmazonSQS sqsClient = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSQS.GetQueueUrlAsync) => GetQueueUrl(args, queueCalls),
            nameof(IAmazonSQS.GetQueueAttributesAsync) => Task.FromResult(new GetQueueAttributesResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                Attributes = new Dictionary<string, string>
                {
                    [QueueAttributeName.QueueArn] = "arn:aws:sqs:eu-central-1:123456789012:test-queue"
                }
            }),
            nameof(IDisposable.Dispose) => null,
            _ => throw new NotSupportedException(method.Name)
        });
        IAmazonSimpleNotificationService snsClient = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, args) => method.Name switch
        {
            nameof(IAmazonSimpleNotificationService.ListTopicsAsync) => Task.FromResult(new ListTopicsResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                Topics = []
            }),
            nameof(IAmazonSimpleNotificationService.CreateTopicAsync) => CreateTopic(args, topicCalls),
            nameof(IAmazonSimpleNotificationService.GetTopicAttributesAsync) => Task.FromResult(new GetTopicAttributesResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                Attributes = new Dictionary<string, string>()
            }),
            nameof(IDisposable.Dispose) => null,
            _ => throw new NotSupportedException(method.Name)
        });
        IConnection connection = new Connection(() => sqsClient, () => snsClient);
        AmazonSqsBusConfiguration bus = CreateBusConfiguration();
        var host = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1"));
        IAmazonSqsHostConfigurator publicHost = host;
        publicHost.ClientContextCache(options);
        bus.HostConfiguration.Settings = host.Settings;
        await using var context = new AmazonSqsConnectionContext(connection, bus.HostConfiguration, CancellationToken.None);

        await context.GetQueueByName("target", CancellationToken.None);
        await context.GetQueueByName("a", CancellationToken.None);
        await context.GetQueueByName("b", CancellationToken.None);
        await context.GetQueueByName("target", CancellationToken.None);
        await context.GetQueueByName("c", CancellationToken.None);
        timeProvider.Advance(advance);
        await context.GetQueueByName("d", CancellationToken.None);
        await context.GetQueueByName("e", CancellationToken.None);
        await context.GetQueueByName("f", CancellationToken.None);
        await context.GetQueueByName("target", CancellationToken.None);

        await ExerciseTopicCache(context, timeProvider, advance);

        return (queueCalls["target"], topicCalls["target"]);
    }

    private static async Task ExerciseTopicCache(AmazonSqsConnectionContext context, FakeTimeProvider timeProvider, TimeSpan advance)
    {
        await context.GetTopic(new TestTopic("target"), CancellationToken.None);
        await context.GetTopic(new TestTopic("a"), CancellationToken.None);
        await context.GetTopic(new TestTopic("b"), CancellationToken.None);
        await context.GetTopic(new TestTopic("target"), CancellationToken.None);
        await context.GetTopic(new TestTopic("c"), CancellationToken.None);
        timeProvider.Advance(advance);
        await context.GetTopic(new TestTopic("d"), CancellationToken.None);
        await context.GetTopic(new TestTopic("e"), CancellationToken.None);
        await context.GetTopic(new TestTopic("f"), CancellationToken.None);
        await context.GetTopic(new TestTopic("target"), CancellationToken.None);
    }

    private static Task<GetQueueUrlResponse> GetQueueUrl(object?[]? args, IDictionary<string, int> calls)
    {
        string queueName = Assert.IsType<string>(args![0]);
        calls.TryGetValue(queueName, out int observedCalls);
        calls[queueName] = observedCalls + 1;
        return Task.FromResult(new GetQueueUrlResponse
        {
            HttpStatusCode = System.Net.HttpStatusCode.OK,
            QueueUrl = $"https://sqs.eu-central-1.amazonaws.com/123456789012/{queueName}"
        });
    }

    private static Task<CreateTopicResponse> CreateTopic(object?[]? args, IDictionary<string, int> calls)
    {
        CreateTopicRequest request = Assert.IsType<CreateTopicRequest>(args![0]);
        calls.TryGetValue(request.Name, out int observedCalls);
        calls[request.Name] = observedCalls + 1;
        return Task.FromResult(new CreateTopicResponse
        {
            HttpStatusCode = System.Net.HttpStatusCode.OK,
            TopicArn = $"arn:aws:sns:eu-central-1:123456789012:{request.Name}"
        });
    }

    private sealed class TestTopic(string entityName) : ViciOne.ServiceBus.AmazonSqsTransport.Topology.Topic
    {
        public string EntityName { get; } = entityName;
        public bool Durable => false;
        public bool AutoDelete => true;
        public IDictionary<string, object> TopicAttributes { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> TopicSubscriptionAttributes { get; } = new Dictionary<string, object>();
        public IDictionary<string, string> TopicTags { get; } = new Dictionary<string, string>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowOnDispose(Exception exception) => throw exception;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IAmazonSimpleNotificationService ThrowFromSnsFactory(Exception exception) => throw exception;
}
