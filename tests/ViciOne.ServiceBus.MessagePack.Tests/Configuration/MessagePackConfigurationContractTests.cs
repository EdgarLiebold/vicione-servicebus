using System.Reflection;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessagePack.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Configuration;

public sealed class MessagePackConfigurationContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PUBLIC-API", "minimal-greenfield-surface")]
    public void PublicApi_ContainsOnlyCompositionAndTheAdvancedFactory()
    {
        string[] exportedTypes =
        [..
            typeof(MessagePackConfigurationExtensions).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(
            [
                "ViciOne.ServiceBus.MessagePack.MessagePackConfigurationExtensions",
                "ViciOne.ServiceBus.MessagePack.MessagePackSerializerFactory",
            ],
            exportedTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-PUBLIC-API", "complete-configuration-overload-matrix")]
    public void ConfigurationApi_ExposesTheCompleteOverloadMatrixWithReviewedDefaults()
    {
        string[] methods =
        [..
            typeof(MessagePackConfigurationExtensions)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(method =>
            {
                System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                return $"{method.Name}({parameters[0].ParameterType.Name}, {parameters[1].Name}={parameters[1].DefaultValue})";
            })
            .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(
            [
                "UseMessagePackDeserializer(IBusFactoryConfigurator, isDefault=False)",
                "UseMessagePackDeserializer(IReceiveEndpointConfigurator, isDefault=False)",
                "UseMessagePackSerializer(IBusFactoryConfigurator, isDefault=True)",
                "UseMessagePackSerializer(IReceiveEndpointConfigurator, isDefault=True)",
            ],
            methods);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CONFIGURATION", "missing-owner-fails-at-extension-boundary")]
    public void ConfigurationExtensions_RejectMissingConfiguratorsWithExactOwnership()
    {
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MessagePackConfigurationExtensions.UseMessagePackSerializer(
                (IReceiveEndpointConfigurator)null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MessagePackConfigurationExtensions.UseMessagePackSerializer(
                (IBusFactoryConfigurator)null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MessagePackConfigurationExtensions.UseMessagePackDeserializer(
                (IBusFactoryConfigurator)null!)).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            MessagePackConfigurationExtensions.UseMessagePackDeserializer(
                (IReceiveEndpointConfigurator)null!)).ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CONFIGURATION", "endpoint-deserializer-registration")]
    public void EndpointDeserializer_RegistersTheFactoryAndForwardsTheDefaultSelection(bool isDefault)
    {
        IReceiveEndpointConfigurator configurator =
            DispatchProxy.Create<IReceiveEndpointConfigurator, RecordingReceiveEndpointConfigurator>();
        var recorder = (RecordingReceiveEndpointConfigurator)configurator;

        configurator.UseMessagePackDeserializer(isDefault);

        Registration registration = Assert.Single(recorder.Registrations);
        Assert.Equal(nameof(IReceiveEndpointConfigurator.AddDeserializer), registration.Operation);
        Assert.Equal(MessagePackMessageSerializer.MessagePackContentType, registration.Factory.ContentType);
        Assert.Equal(isDefault, registration.IsDefault);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CONFIGURATION", "bus-deserializer-registration")]
    public void BusDeserializer_RegistersTheFactoryAndForwardsTheDefaultSelection(bool isDefault)
    {
        IBusFactoryConfigurator configurator =
            DispatchProxy.Create<IBusFactoryConfigurator, RecordingBusFactoryConfigurator>();
        var recorder = (RecordingBusFactoryConfigurator)configurator;

        configurator.UseMessagePackDeserializer(isDefault);

        Registration registration = Assert.Single(recorder.Registrations);
        Assert.Equal(nameof(IBusFactoryConfigurator.AddDeserializer), registration.Operation);
        Assert.Equal(MessagePackMessageSerializer.MessagePackContentType, registration.Factory.ContentType);
        Assert.Equal(isDefault, registration.IsDefault);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CONFIGURATION", "endpoint-bidirectional-registration")]
    public void EndpointSerializer_RegistersOneSharedFactoryForBothDirections(bool isDefault)
    {
        IReceiveEndpointConfigurator configurator =
            DispatchProxy.Create<IReceiveEndpointConfigurator, RecordingReceiveEndpointConfigurator>();
        var recorder = (RecordingReceiveEndpointConfigurator)configurator;

        configurator.UseMessagePackSerializer(isDefault);

        AssertBidirectionalRegistration(recorder.Registrations, isDefault);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CONFIGURATION", "bus-bidirectional-registration")]
    public void BusSerializer_RegistersOneSharedFactoryForBothDirections(bool isDefault)
    {
        IBusFactoryConfigurator configurator =
            DispatchProxy.Create<IBusFactoryConfigurator, RecordingBusFactoryConfigurator>();
        var recorder = (RecordingBusFactoryConfigurator)configurator;

        configurator.UseMessagePackSerializer(isDefault);

        AssertBidirectionalRegistration(recorder.Registrations, isDefault);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DEPENDENCIES", "no-optional-capability-coupling")]
    public void ProductAssembly_DoesNotReferenceOptionalCourierOrJobServiceCapabilities()
    {
        string[] references =
        [..
            typeof(MessagePackSerializerFactory).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Order(StringComparer.Ordinal),
        ];

        Assert.DoesNotContain("ViciOne.ServiceBus.Courier", references);
        Assert.DoesNotContain("ViciOne.ServiceBus.JobService", references);
    }

    private static void AssertBidirectionalRegistration(
        IReadOnlyList<Registration> registrations,
        bool isDefault)
    {
        Assert.Collection(
            registrations,
            serializer =>
            {
                Assert.Equal(nameof(IReceiveEndpointConfigurator.AddSerializer), serializer.Operation);
                Assert.Equal(isDefault, serializer.IsDefault);
                Assert.Equal(MessagePackMessageSerializer.MessagePackContentType, serializer.Factory.ContentType);
            },
            deserializer =>
            {
                Assert.Equal(nameof(IReceiveEndpointConfigurator.AddDeserializer), deserializer.Operation);
                Assert.Equal(isDefault, deserializer.IsDefault);
                Assert.Equal(MessagePackMessageSerializer.MessagePackContentType, deserializer.Factory.ContentType);
            });
        Assert.Same(registrations[0].Factory, registrations[1].Factory);
    }

    private abstract class RecordingConfigurator : DispatchProxy
    {
        public List<Registration> Registrations { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? arguments)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name is not nameof(IReceiveEndpointConfigurator.AddSerializer)
                and not nameof(IReceiveEndpointConfigurator.AddDeserializer))
            {
                throw new InvalidOperationException($"Unexpected configurator call '{targetMethod.Name}'.");
            }

            Assert.NotNull(arguments);
            Registrations.Add(new Registration(
                targetMethod.Name,
                Assert.IsType<ISerializerFactory>(arguments[0], exactMatch: false),
                Assert.IsType<bool>(arguments[1])));
            return null;
        }
    }

    private class RecordingReceiveEndpointConfigurator : RecordingConfigurator;

    private class RecordingBusFactoryConfigurator : RecordingConfigurator;

    private sealed record Registration(string Operation, ISerializerFactory Factory, bool IsDefault);
}
