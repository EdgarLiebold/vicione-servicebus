using System.Reflection;
using System.Reflection.Emit;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsMessageNameFormatterTests
{
    const string CollisionDigest = "FBB91AF754272619582D3586268524FFB8BF293E02BB2EF822C1BAD45ECEF507";

    [Fact]
    public void ClosedMessageTypes_ProduceStableDistinctAwsTopicNames()
    {
        var formatter = new AmazonSqsMessageNameFormatter();

        string invoice = formatter.GetMessageName(typeof(Invoice));
        string payment = formatter.GetMessageName(typeof(Payment));
        string nestedInvoice = formatter.GetMessageName(typeof(Envelope<Invoice>.Item<Payment>));
        string nestedPayment = formatter.GetMessageName(typeof(Envelope<Payment>.Item<Invoice>));

        Assert.Equal("ViciOne_ServiceBus_AmazonSqs_Tests-AmazonSqsMessageNameFormatterTests_Invoice", invoice);
        Assert.Equal("ViciOne_ServiceBus_AmazonSqs_Tests-AmazonSqsMessageNameFormatterTests_Payment", payment);
        Assert.Equal(invoice, formatter.GetMessageName(typeof(Invoice)));
        Assert.Equal(4, new HashSet<string>(StringComparer.Ordinal)
        {
            invoice, payment, nestedInvoice, nestedPayment
        }.Count);
        Assert.All(new[] { invoice, payment, nestedInvoice, nestedPayment },
            name => Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(name), name));
    }

    [Fact]
    public void DistinctNamespaceAndNestedContracts_NeverShareATopicName()
    {
        var formatter = new AmazonSqsMessageNameFormatter();
        Type[] contracts =
        [
            CreateNamedContract("ViciOne.ServiceBus.AmazonSqs.Tests.FormatterContracts.Alpha.Beta.Contract"),
            CreateNamedContract("ViciOne.ServiceBus.AmazonSqs.Tests.FormatterContracts.Alpha_Beta.Contract"),
            typeof(Outer.Inner),
            typeof(Outer_Inner),
        ];

        string[] names = contracts.Select(formatter.GetMessageName).ToArray();

        Assert.Equal(contracts.Length, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("-ViciOne_dServiceBus_dAmazonSqs_dTests_dFormatterContracts_dAlpha_uBeta-Contract", names[1]);
        Assert.Contains("_nOuter_uInner", names[3], StringComparison.Ordinal);
        Assert.All(names, name => Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(name), name));
        Assert.Equal(names[1], new AmazonSqsMessageNameFormatter().GetMessageName(contracts[1]));
        Assert.Equal(names[3], new AmazonSqsMessageNameFormatter().GetMessageName(contracts[3]));
    }

    [Fact]
    public void CustomNamespaceSeparator_CannotMergeASeparatorWithAnIdentifier()
    {
        var formatter = new AmazonSqsMessageNameFormatter(nestedTypeSeparator: "Q");

        Type segmentedContract = CreateNamedContract("ViciOne.ServiceBus.AmazonSqs.Tests.FormatterContracts.Alpha.Beta.Contract");
        Type identifierContract = CreateNamedContract("ViciOne.ServiceBus.AmazonSqs.Tests.FormatterContracts.AlphaQBeta.Contract");
        string segmented = formatter.GetMessageName(segmentedContract);
        string identifier = formatter.GetMessageName(identifierContract);

        Assert.NotEqual(segmented, identifier);
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(segmented));
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(identifier));
        Assert.Equal(identifier, new AmazonSqsMessageNameFormatter(nestedTypeSeparator: "Q")
            .GetMessageName(identifierContract));
    }

    [Fact]
    public void ConfiguredSeparators_ProduceTheRequestedClosedGenericName()
    {
        var formatter = new AmazonSqsMessageNameFormatter(
            includeNamespace: false,
            genericArgumentSeparator: "_",
            genericTypeSeparator: "-",
            namespaceSeparator: "x",
            nestedTypeSeparator: "Q");

        string name = formatter.GetMessageName(typeof(Pair<Invoice, Payment>));

        Assert.Equal("AmazonSqsMessageNameFormatterTestsQPair-AmazonSqsMessageNameFormatterTestsQInvoice_AmazonSqsMessageNameFormatterTestsQPayment-", name);
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(name));
    }

    [Fact]
    public void EmptyAndDuplicateSeparators_AreRejectedBeforeTheyCanMergeContracts()
    {
        Assert.Throws<ArgumentException>(() => new AmazonSqsMessageNameFormatter(genericArgumentSeparator: ""));
        Assert.Throws<ArgumentException>(() => new AmazonSqsMessageNameFormatter(genericArgumentSeparator: "_", nestedTypeSeparator: "_"));
        Assert.Throws<ArgumentException>(() => new AmazonSqsMessageNameFormatter(namespaceSeparator: "-", nestedTypeSeparator: "-"));
        Assert.Throws<ArgumentException>(() => new AmazonSqsMessageNameFormatter(genericTypeSeparator: "__"));

        var withoutNamespace = new AmazonSqsMessageNameFormatter(includeNamespace: false, namespaceSeparator: "_");
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(withoutNamespace.GetMessageName(typeof(Invoice))));
    }

    [Fact]
    public void LongCanonicalNames_StayValidAndRetainDistinctStableSuffixes()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("SqsLongTopicContractTests"), AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule("Contracts");
        Type first = module.DefineType("A" + new string('_', 128) + "B", TypeAttributes.Public).CreateType()!;
        Type second = module.DefineType("A" + new string('_', 128) + "C", TypeAttributes.Public).CreateType()!;
        var formatter = new AmazonSqsMessageNameFormatter();

        string firstName = formatter.GetMessageName(first);
        string secondName = formatter.GetMessageName(second);

        Assert.Equal(256, firstName.Length);
        Assert.Equal(256, secondName.Length);
        Assert.StartsWith("--", firstName, StringComparison.Ordinal);
        Assert.StartsWith("--", secondName, StringComparison.Ordinal);
        Assert.NotEqual(firstName, secondName);
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(firstName));
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(secondName));
        Assert.Equal(firstName, new AmazonSqsMessageNameFormatter().GetMessageName(first));
        Assert.Equal(secondName, new AmazonSqsMessageNameFormatter().GetMessageName(second));
    }

    [Fact]
    public void HashedCanonicalName_CannotEqualAShortCanonicalContract()
    {
        (Type longContract, Type secondLongContract, Type shortContract) = CreateHashCollisionContracts();
        var formatter = new AmazonSqsMessageNameFormatter();

        string longName = formatter.GetMessageName(longContract);
        string secondLongName = formatter.GetMessageName(secondLongContract);
        string shortName = formatter.GetMessageName(shortContract);

        Assert.Equal(256, longName.Length);
        Assert.Equal(256, shortName.Length);
        Assert.StartsWith("--", longName, StringComparison.Ordinal);
        Assert.EndsWith(CollisionDigest, longName, StringComparison.Ordinal);
        Assert.StartsWith("--", secondLongName, StringComparison.Ordinal);
        Assert.StartsWith("-A_u", shortName, StringComparison.Ordinal);
        Assert.NotEqual(longName, secondLongName);
        Assert.NotEqual(longName, shortName);
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(longName));
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(secondLongName));
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(shortName));
    }

    [Fact]
    public void ScopedLongCanonicalContract_UsesAValidDistinctPublishDestination()
    {
        (Type longContract, Type secondLongContract, Type shortContract) = CreateHashCollisionContracts();
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        busConfiguration.HostConfiguration.Settings = CreateScopedSettings("production");

        Uri longDestination = busConfiguration.HostConfiguration.Topology.GetDestinationAddress(longContract);
        Uri secondLongDestination = busConfiguration.HostConfiguration.Topology.GetDestinationAddress(secondLongContract);
        Uri shortDestination = busConfiguration.HostConfiguration.Topology.GetDestinationAddress(shortContract);
        var longAddress = new AmazonSqsEndpointAddress(busConfiguration.HostConfiguration.HostAddress, longDestination);
        var secondLongAddress = new AmazonSqsEndpointAddress(busConfiguration.HostConfiguration.HostAddress, secondLongDestination);
        var shortAddress = new AmazonSqsEndpointAddress(busConfiguration.HostConfiguration.HostAddress, shortDestination);

        Assert.Equal(256, longAddress.Name.Length);
        Assert.Equal(256, secondLongAddress.Name.Length);
        Assert.Equal(256, shortAddress.Name.Length);
        Assert.StartsWith("production_--", longAddress.Name, StringComparison.Ordinal);
        Assert.StartsWith("production_--", secondLongAddress.Name, StringComparison.Ordinal);
        Assert.Equal(longAddress.Name[..50], secondLongAddress.Name[..50]);
        Assert.NotEqual(longAddress.Name, secondLongAddress.Name);
        Assert.NotEqual(longAddress.Name, shortAddress.Name);
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(longAddress.Name));
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(secondLongAddress.Name));
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(shortAddress.Name));
    }

    [Fact]
    public void ScopeThatLeavesNoRoomForADigest_RejectsTheLongTopic()
    {
        Type longContract = CreateHashCollisionContracts().LongContract;
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        busConfiguration.HostConfiguration.Settings = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
            Scope = new string('s', 190),
            ScopeTopics = true,
        }.Freeze();

        AmazonSqsTransportConfigurationException error = Assert.Throws<AmazonSqsTransportConfigurationException>(
            () => busConfiguration.HostConfiguration.Topology.GetDestinationAddress(longContract));

        Assert.Contains("scope is too long", error.Message, StringComparison.Ordinal);
        Assert.True(AmazonSnsTopicNameValidator.Validator.IsValidEntityName(
            new AmazonSqsMessageNameFormatter().GetMessageName(longContract)));
    }

    [Fact]
    public void OpenMessageTypes_AreRejectedBeforeTheyCanNameSharedTopics()
    {
        var formatter = new AmazonSqsMessageNameFormatter();
        Type genericParameter = typeof(Envelope<>).GetGenericArguments()[0];
        Type partiallyOpen = typeof(List<>).MakeGenericType(genericParameter);

        Assert.Equal("type", Assert.Throws<ArgumentException>(() => formatter.GetMessageName(typeof(Envelope<>))).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() => formatter.GetMessageName(genericParameter)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() => formatter.GetMessageName(partiallyOpen)).ParamName);
    }

    [Fact]
    public void PublishTopology_UsesTheContractNameAndRejectsPartiallyOpenContracts()
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        busConfiguration.HostConfiguration.Settings = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
        }.Freeze();

        Type genericParameter = typeof(Envelope<>).GetGenericArguments()[0];
        Type partiallyOpen = typeof(List<>).MakeGenericType(genericParameter);
        Assert.Throws<ArgumentException>(() => busConfiguration.HostConfiguration.Topology.GetDestinationAddress(partiallyOpen));

        Uri destination = busConfiguration.HostConfiguration.Topology.GetDestinationAddress(typeof(Invoice));
        var address = new AmazonSqsEndpointAddress(new Uri("amazonsqs://eu-central-1/"), destination);

        Assert.Equal(AmazonSqsEndpointAddress.AddressType.Topic, address.Type);
        Assert.Equal("ViciOne_ServiceBus_AmazonSqs_Tests-AmazonSqsMessageNameFormatterTests_Invoice", address.Name);
        Assert.True(address.Durable);
        Assert.False(address.AutoDelete);
    }

    [Fact]
    public void ScopedTypeDestination_ResolvesTheActualPublishTopic()
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        busConfiguration.HostConfiguration.Settings = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
            Scope = "production",
            ScopeTopics = true,
        }.Freeze();

        string publishedTopic = ((IAmazonSqsTopologyConfiguration)topologyConfiguration)
            .Publish.GetMessageTopology<Invoice>().Topic.EntityName;
        Uri destination = busConfiguration.HostConfiguration.Topology.GetDestinationAddress(typeof(Invoice));
        var resolved = new AmazonSqsEndpointAddress(busConfiguration.HostConfiguration.HostAddress, destination);

        Assert.Equal("production_ViciOne_ServiceBus_AmazonSqs_Tests-AmazonSqsMessageNameFormatterTests_Invoice", publishedTopic);
        Assert.Equal(publishedTopic, resolved.Name);
        Assert.Equal(AmazonSqsEndpointAddress.AddressType.Topic, resolved.Type);
    }

    [Fact]
    public void RepeatingScopedHostSettings_DoesNotStackThePublishPrefix()
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        AmazonSqsHostSettings settings = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
            Scope = "production",
            ScopeTopics = true,
        }.Freeze();

        busConfiguration.HostConfiguration.Settings = settings;
        busConfiguration.HostConfiguration.Settings = settings;

        string publishedTopic = ((IAmazonSqsTopologyConfiguration)topologyConfiguration)
            .Publish.GetMessageTopology<Invoice>().Topic.EntityName;
        busConfiguration.HostConfiguration.Settings = settings;
        Uri destination = busConfiguration.HostConfiguration.Topology.GetDestinationAddress(typeof(Invoice));
        var resolved = new AmazonSqsEndpointAddress(busConfiguration.HostConfiguration.HostAddress, destination);

        Assert.Equal("production_ViciOne_ServiceBus_AmazonSqs_Tests-AmazonSqsMessageNameFormatterTests_Invoice", publishedTopic);
        Assert.Equal(publishedTopic, resolved.Name);
    }

    [Fact]
    public void ScopedHostAfterPublishConfiguration_IsRejectedBeforeTopicsDiverge()
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        var configurator = new AmazonSqsBusFactoryConfigurator(busConfiguration);
        configurator.Publish(typeof(Invoice));
        AmazonSqsHostSettings settings = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
            Scope = "production",
            ScopeTopics = true,
        }.Freeze();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => configurator.Host(settings));

        Assert.Contains("before message topology", error.Message, StringComparison.Ordinal);
        Assert.Throws<ViciOne.ServiceBus.ConfigurationException>(() => _ = busConfiguration.HostConfiguration.Settings);
        Assert.DoesNotContain("production_", ((IAmazonSqsTopologyConfiguration)topologyConfiguration)
            .Publish.GetMessageTopology<Invoice>().Topic.EntityName, StringComparison.Ordinal);
    }

    [Fact]
    public void ScopedHostAfterMessageTopologyConfiguration_IsRejectedBeforeTopicsDiverge()
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        _ = ((IAmazonSqsTopologyConfiguration)topologyConfiguration).Message.GetMessageTopology<Invoice>();
        AmazonSqsHostSettings settings = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
            Scope = "production",
            ScopeTopics = true,
        }.Freeze();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => busConfiguration.HostConfiguration.Settings = settings);

        Assert.Contains("before message topology", error.Message, StringComparison.Ordinal);
        Assert.Throws<ViciOne.ServiceBus.ConfigurationException>(() => _ = busConfiguration.HostConfiguration.Settings);
        Assert.DoesNotContain("production_", ((IAmazonSqsTopologyConfiguration)topologyConfiguration)
            .Publish.GetMessageTopology<Invoice>().Topic.EntityName, StringComparison.Ordinal);
    }

    [Fact]
    public void ScopedHostAfterPreexistingMessageTopology_IsRejectedBeforeTopicsDiverge()
    {
        var messageTopology = AmazonSqsBusFactory.CreateMessageTopology();
        _ = messageTopology.GetMessageTopology<Invoice>();
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(messageTopology);
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        AmazonSqsHostSettings settings = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
            Scope = "production",
            ScopeTopics = true,
        }.Freeze();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => busConfiguration.HostConfiguration.Settings = settings);

        Assert.Contains("before message topology", error.Message, StringComparison.Ordinal);
        Assert.Throws<ViciOne.ServiceBus.ConfigurationException>(() => _ = busConfiguration.HostConfiguration.Settings);
    }

    [Fact]
    public void ScopedHostWithOpaqueMessageTopology_IsRejectedWithoutChangingTheHost()
    {
        var messageTopology = new DelegatingMessageTopology(AmazonSqsBusFactory.CreateMessageTopology());
        _ = messageTopology.GetMessageTopology<Invoice>();
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(messageTopology);
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        AmazonSqsHostSettings original = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
        }.Freeze();
        busConfiguration.HostConfiguration.Settings = original;
        AmazonSqsHostSettings scoped = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
            Scope = "production",
            ScopeTopics = true,
        }.Freeze();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => busConfiguration.HostConfiguration.Settings = scoped);

        Assert.Contains("built-in message topology", error.Message, StringComparison.Ordinal);
        Assert.Same(original, busConfiguration.HostConfiguration.Settings);
        Assert.DoesNotContain("production_", messageTopology.GetMessageTopology<Invoice>().EntityName, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectPublishTopology_PreventsLateScopeChangesWithoutMutatingHostSettings()
    {
        var topologyConfiguration = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        var busConfiguration = new AmazonSqsBusConfiguration(topologyConfiguration);
        AmazonSqsHostSettings original = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
            Scope = "production",
            ScopeTopics = true,
        }.Freeze();
        busConfiguration.HostConfiguration.Settings = original;
        _ = ((IAmazonSqsTopologyConfiguration)topologyConfiguration).Publish.GetMessageTopology<Invoice>();
        AmazonSqsHostSettings changed = new ConfigurationHostSettings
        {
            Region = global::Amazon.RegionEndpoint.EUCentral1,
            Scope = "staging",
            ScopeTopics = true,
        }.Freeze();

        Assert.Throws<InvalidOperationException>(() => busConfiguration.HostConfiguration.Settings = changed);

        Assert.Same(original, busConfiguration.HostConfiguration.Settings);
        Assert.Equal("production_ViciOne_ServiceBus_AmazonSqs_Tests-AmazonSqsMessageNameFormatterTests_Invoice",
            ((IAmazonSqsTopologyConfiguration)topologyConfiguration).Publish.GetMessageTopology<Invoice>().Topic.EntityName);
    }

    public sealed class Invoice;

    public sealed class Payment;

    private sealed class Pair<TLeft, TRight>;

    private sealed class Envelope<T>
    {
        public sealed class Item<TItem>;
    }

    private sealed class Outer
    {
        public sealed class Inner;
    }

    private sealed class Outer_Inner;

    static Type CreateNamedContract(string fullName)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("SqsSeparatorContracts"), AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule("Contracts");
        return module.DefineType(fullName, TypeAttributes.Public).CreateType()!;
    }

    static AmazonSqsHostSettings CreateScopedSettings(string scope) => new ConfigurationHostSettings
    {
        Region = global::Amazon.RegionEndpoint.EUCentral1,
        Scope = scope,
        ScopeTopics = true,
    }.Freeze();

    static (Type LongContract, Type SecondLongContract, Type ShortContract) CreateHashCollisionContracts()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("SqsHashCollisionContracts"), AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule("Contracts");
        string contractNamespace = "A" + new string('_', 94) + "B";
        Type longContract = module.DefineType(contractNamespace + "." + new string('L', 100) + "0", TypeAttributes.Public).CreateType()!;
        Type secondLongContract = module.DefineType(contractNamespace + "." + new string('L', 100) + "1", TypeAttributes.Public).CreateType()!;
        Type shortContract = module.DefineType(contractNamespace + "." + CollisionDigest, TypeAttributes.Public).CreateType()!;
        return (longContract, secondLongContract, shortContract);
    }

    sealed class DelegatingMessageTopology : IMessageTopologyConfigurator
    {
        readonly IMessageTopologyConfigurator _inner;

        public DelegatingMessageTopology(IMessageTopologyConfigurator inner)
        {
            _inner = inner;
        }

        public IEntityNameFormatter EntityNameFormatter => _inner.EntityNameFormatter;

        public void SetEntityNameFormatter(IEntityNameFormatter formatter) => _inner.SetEntityNameFormatter(formatter);

        public IMessageTopologyConfigurator<T> GetMessageTopology<T>() where T : class => _inner.GetMessageTopology<T>();

        IMessageTopology<T> IMessageTopology.GetMessageTopology<T>() => _inner.GetMessageTopology<T>();

        public ConnectHandle ConnectMessageTopologyConfigurationObserver(IMessageTopologyConfigurationObserver observer)
            => _inner.ConnectMessageTopologyConfigurationObserver(observer);
    }
}
