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

        MessagePackSerializerFactory factory = Assert.IsType<MessagePackSerializerFactory>(recorder.Factory);
        Assert.Equal(MessagePackMessageSerializer.MessagePackContentType, factory.ContentType);
        Assert.Equal(isDefault, recorder.IsDefault);
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

    private class RecordingReceiveEndpointConfigurator : DispatchProxy
    {
        public ISerializerFactory? Factory { get; private set; }

        public bool? IsDefault { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? arguments)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name != nameof(IReceiveEndpointConfigurator.AddDeserializer))
                throw new InvalidOperationException($"Unexpected configurator call '{targetMethod.Name}'.");

            Assert.NotNull(arguments);
            Factory = Assert.IsType<ISerializerFactory>(arguments[0], exactMatch: false);
            IsDefault = Assert.IsType<bool>(arguments[1]);
            return null;
        }
    }
}
