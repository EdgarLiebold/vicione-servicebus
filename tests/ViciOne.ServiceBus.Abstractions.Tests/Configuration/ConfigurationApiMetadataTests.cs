using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Configuration;

public sealed class ConfigurationApiMetadataTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-API", "descriptive-generic-metadata")]
    public void ConfigurationContracts_ExposeDescriptiveGenericParameterNames()
    {
        AssertGenericMethods(typeof(IConsumeTopologyConfigurator), nameof(IConsumeTopologyConfigurator.GetMessageTopology), "TMessage");
        AssertGenericMethods(typeof(IPublishTopologyConfigurator), nameof(IPublishTopologyConfigurator.GetMessageTopology), "TMessage");
        AssertGenericMethods(typeof(IPublishTopologyConfigurator), nameof(IPublishTopologyConfigurator.AddMessagePublishTopology), "TMessage");
        AssertGenericMethods(typeof(ISendTopologyConfigurator), nameof(ISendTopologyConfigurator.AddMessageSendTopology), "TMessage");
        AssertGenericMethods(typeof(IConsumePipeConfigurator), nameof(IConsumePipeConfigurator.AddPipeSpecification), "TMessage");
        AssertGenericMethods(typeof(IPublishPipeConfigurator), nameof(IPublishPipeConfigurator.AddPipeSpecification), "TMessage");
        AssertGenericMethods(typeof(ISendPipeConfigurator), nameof(ISendPipeConfigurator.AddPipeSpecification), "TMessage");
        AssertGenericMethods(typeof(IReceiveEndpointConfigurator), nameof(IReceiveEndpointConfigurator.ConfigureMessageTopology), "TMessage");
        AssertGenericMethods(typeof(IExceptionConfigurator), nameof(IExceptionConfigurator.Handle), "TException");
        AssertGenericMethods(typeof(IExceptionConfigurator), nameof(IExceptionConfigurator.Ignore), "TException");

        MethodInfo[] filterMethods = typeof(FilterConfigurationExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name is nameof(FilterConfigurationExtensions.UseFilter) or nameof(FilterConfigurationExtensions.UseFilters))
            .ToArray();
        Assert.NotEmpty(filterMethods);
        Assert.All(filterMethods, method =>
        {
            string[] expected = method.GetGenericArguments().Length == 2
                ? ["TContext", "TFilter"]
                : method.GetParameters()[0].ParameterType == typeof(IConsumePipeConfigurator)
                    || method.GetParameters()[0].ParameterType == typeof(ISendPipeConfigurator)
                    || method.GetParameters()[0].ParameterType == typeof(IPublishPipeConfigurator)
                        ? ["TMessage"]
                        : ["TContext"];
            Assert.Equal(expected, method.GetGenericArguments().Select(argument => argument.Name));
        });
    }

    private static void AssertGenericMethods(Type declaringType, string methodName, params string[] expectedNames)
    {
        MethodInfo[] methods = declaringType.GetMethods()
            .Where(method => method.Name == methodName && method.IsGenericMethod)
            .ToArray();

        Assert.NotEmpty(methods);
        Assert.All(methods, method =>
            Assert.Equal(expectedNames, method.GetGenericArguments().Select(argument => argument.Name)));
    }
}
