using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Architecture;

public sealed class RemovalBoundaryTests
{
    private static readonly string[] RemovedNamespaces =
    [
        "ViciOne.ServiceBus.Licensing",
        "ViciOne.ServiceBus.UsageTelemetry",
        "ViciOne.ServiceBus.UsageTracking",
    ];

    private static readonly string[] RemovedTypeNames =
    [
        "BusUsageTelemetry",
        "EndpointUsageTelemetry",
        "HostUsageTelemetry",
        "FaultHandlerConnectHandle",
        "JsonMessageBody",
        "IProbeResultBuilder",
        "IScheduleTokenIdCache`1",
        "IUsageTelemetrySource",
        "IUsageTracker",
        "InvalidLicenseException",
        "InvalidLicenseFormatException",
        "LicenseContact",
        "LicenseCustomer",
        "LicenseFeature",
        "LicenseFile",
        "LicenseInfo",
        "LicenseProduct",
        "LicenseReader",
        "LicenseSettings",
        "PipeContextHandle`1",
        "PublishEndpointRecurringSchedulerExtensions",
        "ProbeResult",
        "ResponseHandlerConfigurator`1",
        "RiderUsageTelemetry",
        "SendEndpointRecurringSchedulerExtensions",
        "UsageTelemetryBusObserver",
        "UsageTelemetryConfigurationObserver",
        "UsageTelemetryEndpointConfigurationObserver",
        "UsageTelemetryOptions",
        "UsageTelemetryOptionsExtensions",
        "UsageTelemetrySerializerContext",
        "UsageTracker",
        "V5ServiceBusInstrumentation`1",
        "ViciOneServiceBusUsageTelemetry",
        "ViciOneServiceBusUsageTelemetryExtensions",
    ];

    private static readonly string[] RemovedConfigurationKeys =
    [
        "UseLicenseFile",
        "VICIONE_SERVICEBUS_LICENSE",
        "VICIONE_SERVICEBUS_LICENSE_PATH",
    ];

    private const string RetainedNeighbour = "ResourceCache`1";

    private static readonly string[] InternalImplementationTypes =
    [
        "ViciOne.ServiceBus.Internals.GraphValidation.AdjacencyList`2",
        "ViciOne.ServiceBus.Internals.CodePrinter",
        "ViciOne.ServiceBus.Internals.CompilerFlags",
        "ViciOne.ServiceBus.Internals.GraphValidation.CyclicGraphException",
        "ViciOne.ServiceBus.Internals.GraphValidation.DependencyGraph`1",
        "ViciOne.ServiceBus.Internals.GraphValidation.DependencyGraphNode`1",
        "ViciOne.ServiceBus.Internals.DictionaryExtensions",
        "ViciOne.ServiceBus.Internals.DynamicImplementationBuilder",
        "ViciOne.ServiceBus.Internals.GraphValidation.Edge`2",
        "ViciOne.ServiceBus.Internals.ExceptionExtensions",
        "ViciOne.ServiceBus.Internals.ExpressionCompiler",
        "ViciOne.ServiceBus.Internals.ExpressionExtensions",
        "ViciOne.ServiceBus.Internals.IDelegateDebugInfo",
        "ViciOne.ServiceBus.Internals.IImplementationBuilder",
        "ViciOne.ServiceBus.Internals.IReadProperty`1",
        "ViciOne.ServiceBus.Internals.IReadProperty`2",
        "ViciOne.ServiceBus.Internals.IReadPropertyCache`1",
        "ViciOne.ServiceBus.Internals.GraphValidation.ITarjanNodeProperties",
        "ViciOne.ServiceBus.Internals.GraphValidation.ITopologicalSortNodeProperties",
        "ViciOne.ServiceBus.Internals.ITypeCache`1",
        "ViciOne.ServiceBus.Internals.IWriteProperty`1",
        "ViciOne.ServiceBus.Internals.IWriteProperty`2",
        "ViciOne.ServiceBus.Internals.IWritePropertyCache`1",
        "ViciOne.ServiceBus.Internals.ILGeneratorHacks",
        "ViciOne.ServiceBus.Internals.GraphValidation.Node`1",
        "ViciOne.ServiceBus.Internals.GraphValidation.NodeList`2",
        "ViciOne.ServiceBus.Internals.GraphValidation.NodeTable`1",
        "ViciOne.ServiceBus.Internals.NotSupported",
        "ViciOne.ServiceBus.Internals.NotSupportedExpressionException",
        "ViciOne.ServiceBus.Internals.QueryStringExtensions",
        "ViciOne.ServiceBus.Internals.ReadProperty`2",
        "ViciOne.ServiceBus.Internals.ReadPropertyCache`1",
        "ViciOne.ServiceBus.Internals.GraphValidation.Tarjan`2",
        "ViciOne.ServiceBus.Internals.TaskExtensions",
        "ViciOne.ServiceBus.Internals.TimeSpanExtensions",
        "ViciOne.ServiceBus.Internals.ToCSharpPrinter",
        "ViciOne.ServiceBus.Internals.ToExpressionPrinter",
        "ViciOne.ServiceBus.Internals.GraphValidation.TopologicalSort`2",
        "ViciOne.ServiceBus.Internals.TryPrintConstant",
        "ViciOne.ServiceBus.Internals.TypeExtensions",
        "ViciOne.ServiceBus.Internals.TypeNameFormatter",
        "ViciOne.ServiceBus.Internals.TypeRelationshipExtensions",
        "ViciOne.ServiceBus.Internals.WriteProperty`2",
        "ViciOne.ServiceBus.Internals.WritePropertyCache`1",
        "ViciOne.ServiceBus.Events.BusReadyEvent",
        "ViciOne.ServiceBus.Events.FaultEvent",
        "ViciOne.ServiceBus.Events.FaultEvent`1",
        "ViciOne.ServiceBus.Events.FaultExceptionInfo",
        "ViciOne.ServiceBus.Events.HostReadyEvent",
        "ViciOne.ServiceBus.Events.ReceiveEndpointCompletedEvent",
        "ViciOne.ServiceBus.Events.ReceiveEndpointFaultedEvent",
        "ViciOne.ServiceBus.Events.ReceiveEndpointReadyEvent",
        "ViciOne.ServiceBus.Events.ReceiveEndpointStoppingEvent",
        "ViciOne.ServiceBus.Events.ReceiveFaultEvent",
        "ViciOne.ServiceBus.Events.ReceiveTransportCompletedEvent",
        "ViciOne.ServiceBus.Events.ReceiveTransportFaultedEvent",
        "ViciOne.ServiceBus.Events.ReceiveTransportReadyEvent",
        "ViciOne.ServiceBus.Clients.BusClientFactoryContext",
        "ViciOne.ServiceBus.Clients.ClientRequestHandle`1",
        "ViciOne.ServiceBus.Clients.HostReceiveEndpointClientFactoryContext",
        "ViciOne.ServiceBus.Clients.MessageResponse`1",
        "ViciOne.ServiceBus.Clients.PublishRequestSendEndpoint`1",
        "ViciOne.ServiceBus.Clients.ReceiveEndpointClientFactoryContext",
        "ViciOne.ServiceBus.Clients.ReceiveEndpointPublishRequestSendEndpoint`1",
        "ViciOne.ServiceBus.Clients.ReceiveEndpointSendRequestSendEndpoint`1",
        "ViciOne.ServiceBus.Clients.RequestClient`1",
        "ViciOne.ServiceBus.Clients.RequestSendEndpoint`1",
        "ViciOne.ServiceBus.Clients.ResponseHandlerConnectHandle`1",
        "ViciOne.ServiceBus.Clients.SendRequestSendEndpoint`1",
        "ViciOne.ServiceBus.Operations.ProbeResultBuilder",
        "ViciOne.ServiceBus.Operations.ScopeProbeContext",
        "ViciOne.ServiceBus.Scheduling.MessageSchedulerConverterCache",
        "ViciOne.ServiceBus.Scheduling.ScheduleTokenIdCache`1",
        "ViciOne.ServiceBus.Metadata.ITypeMetadataCache`1",
        "ViciOne.ServiceBus.Metadata.RegistrationMetadata",
        "ViciOne.ServiceBus.Monitoring.BusHealthCheck",
        "ViciOne.ServiceBus.Monitoring.ConfigureBusHealthCheckServiceOptions",
        "ViciOne.ServiceBus.Topology.NullablePropertyMessageCorrelationId`1",
        "ViciOne.ServiceBus.Topology.PropertyMessageCorrelationId`1",
        "ViciOne.ServiceBus.Transformation.ConsumeTransformContext`1",
        "ViciOne.ServiceBus.Transformation.DelegatePropertyProvider`2",
        "ViciOne.ServiceBus.Transformation.MessageTransformConvention`1",
        "ViciOne.ServiceBus.Transformation.MessageTransformPropertyContext`2",
        "ViciOne.ServiceBus.Transformation.PropertyTransformContext`2",
        "ViciOne.ServiceBus.Transformation.ReplaceMessageFactory`1",
        "ViciOne.ServiceBus.Transformation.SendTransformContext`1",
        "ViciOne.ServiceBus.Transformation.TransformPropertyConverter`1",
        "ViciOne.ServiceBus.Transformation.TransformPropertyInitializer`3",
    ];

    private static readonly string[] RetiredPropertyMetadataTypes =
    [
        "ViciOne.ServiceBus.Internals.IReadOnlyPropertyCache`1",
        "ViciOne.ServiceBus.Internals.IReadWritePropertyCache`1",
        "ViciOne.ServiceBus.Internals.ReadOnlyProperty",
        "ViciOne.ServiceBus.Internals.ReadOnlyProperty`1",
        "ViciOne.ServiceBus.Internals.ReadOnlyProperty`2",
        "ViciOne.ServiceBus.Internals.ReadOnlyPropertyCache`1",
        "ViciOne.ServiceBus.Internals.ReadWriteProperty",
        "ViciOne.ServiceBus.Internals.ReadWriteProperty`1",
        "ViciOne.ServiceBus.Internals.ReadWriteProperty`2",
        "ViciOne.ServiceBus.Internals.ReadWritePropertyCache`1",
    ];

    private static readonly string[] OwnedPropertyMetadataTypes =
    [
        "ViciOne.ServiceBus.Metadata.IReadOnlyPropertyCache`1",
        "ViciOne.ServiceBus.Metadata.IReadWritePropertyCache`1",
        "ViciOne.ServiceBus.Metadata.PropertyAccessPolicy",
        "ViciOne.ServiceBus.Metadata.ReadOnlyProperty",
        "ViciOne.ServiceBus.Metadata.ReadOnlyProperty`1",
        "ViciOne.ServiceBus.Metadata.ReadOnlyProperty`2",
        "ViciOne.ServiceBus.Metadata.ReadOnlyPropertyCache`1",
        "ViciOne.ServiceBus.Metadata.ReadWriteProperty",
        "ViciOne.ServiceBus.Metadata.ReadWriteProperty`1",
        "ViciOne.ServiceBus.Metadata.ReadWriteProperty`2",
        "ViciOne.ServiceBus.Metadata.ReadWritePropertyCache`1",
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-REMOVED-CLOSURES", "metadata-namespaces-types-and-retained-control")]
    public void ProductMetadata_ContainsNoRemovedTypeAndStillContainsTheRetainedResourceCache()
    {
        Type[] types = ProductAssemblies().SelectMany(assembly => assembly.GetTypes()).ToArray();

        string[] namespaceOffenders = types
            .Where(type => RemovedNamespaces.Contains(type.Namespace, StringComparer.Ordinal))
            .Select(type => Assert.IsType<string>(type.FullName))
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] nameOffenders = types
            .Where(type => RemovedTypeNames.Contains(type.Name, StringComparer.Ordinal))
            .Select(type => Assert.IsType<string>(type.FullName))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(namespaceOffenders);
        Assert.Empty(nameOffenders);
        Assert.Contains(types, type => type.Name == RetainedNeighbour);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REMOVED-CLOSURES", "default-container-has-no-removed-registration")]
    public void DefaultRegistration_ContainsNoRemovedServiceOrImplementationType()
    {
        IServiceCollection services = new ServiceCollection()
            .AddViciOneServiceBus(configuration => configuration.UsingInMemory());

        Type[] registeredTypes = services
            .SelectMany(descriptor => new[] { descriptor.ServiceType, descriptor.ImplementationType })
            .Where(type => type is not null)
            .Cast<Type>()
            .ToArray();
        string[] offenders = registeredTypes
            .Where(type => RemovedNamespaces.Contains(type.Namespace, StringComparer.Ordinal)
                || RemovedTypeNames.Contains(type.Name, StringComparer.Ordinal))
            .Select(type => Assert.IsType<string>(type.FullName))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(services);
        Assert.Empty(offenders);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REMOVED-CLOSURES", "assembly-bytes-and-retained-control")]
    public void ProductAssemblyBytes_ContainNoRemovedIdentityAndDoContainTheRetainedNeighbour()
    {
        string[] strings = ProductAssemblies().SelectMany(AssemblyStrings).Distinct(StringComparer.Ordinal).ToArray();
        string[] removed = RemovedTypeNames.Concat(RemovedConfigurationKeys).Concat(RemovedNamespaces).ToArray();

        string[] offenders = strings.Where(text => removed.Contains(text, StringComparer.Ordinal)).ToArray();

        Assert.Empty(offenders);
        Assert.Contains(RetainedNeighbour, strings);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REMOVED-CLOSURES", "two-distinct-product-assemblies")]
    public void RemovalScans_ReadBothDistinctProductAssemblies()
    {
        Assembly[] assemblies = ProductAssemblies().ToArray();
        string[] names = assemblies.Select(assembly => Assert.IsType<string>(assembly.GetName().Name)).ToArray();

        Assert.Equal(2, assemblies.Length);
        Assert.Equal(2, names.Distinct(StringComparer.Ordinal).Count());
        Assert.All(assemblies, assembly => Assert.NotEmpty(assembly.GetTypes()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-METADATA-OWNER", "single-metadata-namespace")]
    public void PropertyMetadataTypes_HaveOneExplicitNamespaceOwner()
    {
        Type[] types = ProductAssemblies().SelectMany(assembly => assembly.GetTypes()).ToArray();
        string[] names = types.Select(type => Assert.IsType<string>(type.FullName)).ToArray();

        string[] retired = names.Intersect(RetiredPropertyMetadataTypes, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        string[] owned = names.Intersect(OwnedPropertyMetadataTypes, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        Assert.Empty(retired);
        Assert.Equal(OwnedPropertyMetadataTypes.Order(StringComparer.Ordinal), owned);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLIC-SURFACE-OWNERSHIP", "implementation-helpers-are-not-exported")]
    public void ImplementationHelpers_AreNotPartOfThePublicProductSurface()
    {
        string[] exported = ProductAssemblies()
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Select(type => Assert.IsType<string>(type.FullName))
            .ToArray();

        Assert.Empty(exported.Intersect(InternalImplementationTypes, StringComparer.Ordinal));
        Assert.DoesNotContain("ViciOne.ServiceBus.Internals.DateTimeConstants", exported);
        Assert.Null(typeof(ViciOne.ServiceBus.Metadata.TypeMetadataCache).GetProperty(
            "ImplementationBuilder",
            BindingFlags.Public | BindingFlags.Static));
    }

    internal static IEnumerable<Assembly> ProductAssemblies()
    {
        yield return typeof(IBus).Assembly;
        yield return typeof(InMemoryConfigurationExtensions).Assembly;
    }

    internal static IEnumerable<string> AssemblyStrings(Assembly assembly)
    {
        byte[] bytes = File.ReadAllBytes(assembly.Location);
        var builder = new System.Text.StringBuilder();

        foreach (byte value in bytes)
        {
            if (value is >= 0x20 and < 0x7f)
            {
                builder.Append((char)value);
                continue;
            }

            if (builder.Length > 0)
                yield return builder.ToString();
            builder.Clear();
        }

        if (builder.Length > 0)
            yield return builder.ToString();
    }
}
