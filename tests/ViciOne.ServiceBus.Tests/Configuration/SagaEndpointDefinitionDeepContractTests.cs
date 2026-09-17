using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaEndpointDefinitionDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ENDPOINT-DEFINITION", "exact-public-surface-and-generic-constraint")]
    public void PublicSurface_HasOneSettingsConstructorAndTheExactSagaConstraint()
    {
        Type openType = typeof(SagaEndpointDefinition<>);
        Type sagaType = Assert.Single(openType.GetGenericArguments());

        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            sagaType.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Collection(sagaType.GetGenericParameterConstraints(), constraint => Assert.Equal(typeof(ISaga), constraint));
        ConstructorInfo constructor = Assert.Single(openType.GetConstructors(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        ParameterInfo parameter = Assert.Single(constructor.GetParameters());
        Assert.Equal("settings", parameter.Name);
        Assert.Equal(
            typeof(IEndpointSettings<>).MakeGenericType(typeof(IEndpointDefinition<>).MakeGenericType(sagaType)),
            parameter.ParameterType);

        MethodInfo format = Assert.Single(openType.GetMethods(
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal("FormatEndpointName", format.Name);
        Assert.True(format.IsFamily);
        Assert.Equal(typeof(string), format.ReturnType);
        Assert.Equal(typeof(IEndpointNameFormatter), Assert.Single(format.GetParameters()).ParameterType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ENDPOINT-DEFINITION", "settings-and-formatter-local-null-boundaries")]
    public void RequiredInputs_AreRejectedLocallyWithStableParameterNames()
    {
        AssertParameter("settings", () => new InspectableEndpointDefinition(null!));
        IEndpointSettings<IEndpointDefinition<EndpointSaga>> settings =
            Proxy<IEndpointSettings<IEndpointDefinition<EndpointSaga>>>(out _);
        var definition = new InspectableEndpointDefinition(settings);

        AssertParameter("formatter", () => definition.Format(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-ENDPOINT-DEFINITION", "formatter-saga-type-identity-and-result")]
    public void FormatEndpointName_ForwardsTheExactSagaTypeAndReturnsTheFormatterResult()
    {
        IEndpointSettings<IEndpointDefinition<EndpointSaga>> settings =
            Proxy<IEndpointSettings<IEndpointDefinition<EndpointSaga>>>(out _);
        IEndpointNameFormatter formatter = Proxy<IEndpointNameFormatter>(out RecordingProxy formatterRecorder);
        formatterRecorder.Handler = (method, _) =>
        {
            Assert.Equal("Saga", method.Name);
            Assert.Equal(typeof(EndpointSaga), Assert.Single(method.GetGenericArguments()));
            return "saga-endpoint";
        };
        var definition = new InspectableEndpointDefinition(settings);

        string result = definition.Format(formatter);

        Assert.Equal("saga-endpoint", result);
        Assert.Equal(1, formatterRecorder.Calls);
    }

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static T Proxy<T>(out RecordingProxy recorder)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, RecordingProxy>();
        recorder = (RecordingProxy)(object)proxy;
        return proxy;
    }

    private class RecordingProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?>? Handler { get; set; }

        public int Calls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            Calls++;
            if (Handler != null)
                return Handler(targetMethod, args);

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }

    private sealed class InspectableEndpointDefinition(
        IEndpointSettings<IEndpointDefinition<EndpointSaga>> settings) : SagaEndpointDefinition<EndpointSaga>(settings)
    {
        public string Format(IEndpointNameFormatter formatter) => FormatEndpointName(formatter);
    }

    public sealed class EndpointSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
