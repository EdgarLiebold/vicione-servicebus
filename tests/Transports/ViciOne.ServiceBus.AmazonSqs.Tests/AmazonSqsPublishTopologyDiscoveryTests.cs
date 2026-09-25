using System.Reflection;
using System.Reflection.Emit;
using ViciOne.ServiceBus.AmazonSqs.Tests.PublishDiscoveryFixtures;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests
{
    public sealed class AmazonSqsPublishTopologyDiscoveryTests
    {
        [Fact]
        [RequirementCoverage("REQ-VSB-AWS-SNS-TOPOLOGY", "namespace-discovery-selects-valid-contracts-and-preserves-callback-identity")]
        public void NamespaceDiscovery_PublishesOnlySelectedClosedContractsWithMatchingCallbacks()
        {
            var published = new List<Type>();
            var configured = new List<Type>();
            IAmazonSqsBusFactoryConfigurator configurator = RecordingConfigurator(published);
            Type[] selected = [typeof(IScanMessage), typeof(ScanMessage), typeof(ScanGenericMessage<>)];

            configurator.AddPublishMessageTypesFromNamespaceContaining<ScanMessage>(
                (_, type) =>
                {
                    Assert.Equal(published[^1], type);
                    configured.Add(type);
                },
                type => selected.Contains(type));

            Assert.Equal([typeof(IScanMessage), typeof(ScanMessage)], published.OrderBy(x => x.Name));
            Assert.Equal(published.OrderBy(x => x.Name), configured.OrderBy(x => x.Name));
            Assert.DoesNotContain(typeof(ScanGenericMessage<>), published);
            Assert.DoesNotContain(typeof(ScanExcludedMessage), published);
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-AWS-SNS-TOPOLOGY", "explicit-publish-types-preserve-order-and-callback-identity")]
        public void ExplicitTypes_PreserveInputOrderAndCallbackIdentity()
        {
            var published = new List<Type>();
            var configured = new List<Type>();
            IAmazonSqsBusFactoryConfigurator configurator = RecordingConfigurator(published);
            Type[] types = [typeof(ScanMessage), typeof(IScanMessage)];

            configurator.AddPublishMessageTypes(types, (_, type) =>
            {
                Assert.Equal(published[^1], type);
                configured.Add(type);
            });

            Assert.Equal(types, published);
            Assert.Equal(types, configured);
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-AWS-SNS-TOPOLOGY", "namespace-discovery-rejects-missing-configurator-and-marker")]
        public void NamespaceDiscovery_RejectsMissingConfiguratorAndMarkerBeforeScanning()
        {
            var published = new List<Type>();
            IAmazonSqsBusFactoryConfigurator configurator = RecordingConfigurator(published);
            Type namespaceLess = AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName($"SqsPublishBoundary{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run)
                .DefineDynamicModule("BoundaryTypes")
                .DefineType("NamespaceLessMessage", TypeAttributes.Public)
                .CreateType()!;

            Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
                AmazonSqsPublishTopologyConfigurationExtensions.AddPublishMessageTypesFromNamespaceContaining<ScanMessage>(null!)).ParamName);
            Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
                AmazonSqsPublishTopologyConfigurationExtensions.AddPublishMessageTypesFromNamespaceContaining(
                    null!, typeof(ScanMessage))).ParamName);
            Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
                configurator.AddPublishMessageTypesFromNamespaceContaining(null!)).ParamName);
            Assert.Equal("type", Assert.Throws<ArgumentException>(() =>
                configurator.AddPublishMessageTypesFromNamespaceContaining(namespaceLess)).ParamName);
            Assert.Empty(published);
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-AWS-SNS-TOPOLOGY", "invalid-explicit-publish-list-has-no-partial-registration")]
        public void InvalidExplicitTypes_RejectBeforeAnyPublishRegistration()
        {
            var published = new List<Type>();
            IAmazonSqsBusFactoryConfigurator configurator = RecordingConfigurator(published);

            Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
                AmazonSqsPublishTopologyConfigurationExtensions.AddPublishMessageTypes(
                    null!, [typeof(ScanMessage)])).ParamName);
            Assert.Equal("messageTypes", Assert.Throws<ArgumentNullException>(() =>
                configurator.AddPublishMessageTypes(null!)).ParamName);
            Assert.Equal("messageTypes", Assert.Throws<ArgumentException>(() =>
                configurator.AddPublishMessageTypes([typeof(ScanMessage), null!])).ParamName);
            Assert.Equal("messageTypes", Assert.Throws<ArgumentException>(() =>
                configurator.AddPublishMessageTypes([typeof(ScanMessage), typeof(string)])).ParamName);
            Assert.Empty(published);
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-AWS-SNS-TOPOLOGY", "namespace-filter-failure-has-no-partial-registration")]
        public void NamespaceFilterFailure_DoesNotPartlyRegisterDiscoveredMessages()
        {
            var published = new List<Type>();
            IAmazonSqsBusFactoryConfigurator configurator = RecordingConfigurator(published);
            var expected = new InvalidOperationException("filter failed");
            var examined = 0;

            InvalidOperationException observed = Assert.Throws<InvalidOperationException>(() =>
                configurator.AddPublishMessageTypesFromNamespaceContaining(
                    typeof(ScanMessage),
                    filter: _ => ++examined == 2 ? throw expected : true));

            Assert.Same(expected, observed);
            Assert.Equal(2, examined);
            Assert.Empty(published);
        }

        private static IAmazonSqsBusFactoryConfigurator RecordingConfigurator(ICollection<Type> published)
        {
            IAmazonSqsMessagePublishTopologyConfigurator topology =
                InterfaceProxy<IAmazonSqsMessagePublishTopologyConfigurator>.Create(
                    (method, _) => throw new NotSupportedException(method.Name));
            return InterfaceProxy<IAmazonSqsBusFactoryConfigurator>.Create((method, args) => method.Name switch
            {
                nameof(IAmazonSqsBusFactoryConfigurator.Publish) => Publish(args),
                _ => throw new NotSupportedException(method.Name)
            });

            object? Publish(object?[]? args)
            {
                Type messageType = Assert.IsAssignableFrom<Type>(args![0]);
                published.Add(messageType);
                (args[1] as Action<IAmazonSqsMessagePublishTopologyConfigurator>)?.Invoke(topology);
                return null;
            }
        }
    }
}

namespace ViciOne.ServiceBus.AmazonSqs.Tests.PublishDiscoveryFixtures
{
    public interface IScanMessage;
    public sealed record ScanMessage;
    public sealed record ScanGenericMessage<T>;
    public sealed record ScanExcludedMessage;
}
