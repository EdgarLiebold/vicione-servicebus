using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaDefinitionDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DEFINITION", "exact-public-surface-and-generic-constraint")]
    public void PublicSurface_ExposesOnlyTheDefinitionAndConcurrencyProperties()
    {
        Type openType = typeof(SagaDefinition<>);
        Type sagaType = Assert.Single(openType.GetGenericArguments());

        Assert.Equal(GenericParameterAttributes.ReferenceTypeConstraint,
            sagaType.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask);
        Assert.Collection(sagaType.GetGenericParameterConstraints(), constraint => Assert.Equal(typeof(ISaga), constraint));
        Assert.Empty(openType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));

        PropertyInfo[] properties = openType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .OrderBy(property => property.Name)
            .ToArray();
        Assert.Collection(
            properties,
            property =>
            {
                Assert.Equal(nameof(SagaDefinition<DefinitionSaga>.ConcurrentMessageLimit), property.Name);
                Assert.Equal(typeof(int?), property.PropertyType);
                Assert.True(property.GetMethod!.IsPublic);
                Assert.True(property.SetMethod!.IsFamily);
            },
            property =>
            {
                Assert.Equal(nameof(SagaDefinition<DefinitionSaga>.EndpointDefinition), property.Name);
                Assert.Equal(typeof(IEndpointDefinition<>).MakeGenericType(sagaType), property.PropertyType);
                Assert.True(property.GetMethod!.IsPublic);
                Assert.True(property.SetMethod!.IsPublic);
            });

        var definition = new InspectableDefinition();
        Assert.Equal(typeof(DefinitionSaga), ((ISagaDefinition)definition).SagaType);
        Assert.Null(((ISagaDefinition)definition).EndpointDefinition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DEFINITION", "configure-required-collaborator-identity-and-order")]
    public void Configure_GuardsEveryCollaboratorBeforeApplyingTheLimitThenForwardsExactIdentities()
    {
        var definition = new InspectableDefinition();
        definition.SetConcurrentMessageLimit(7);
        IReceiveEndpointConfigurator endpoint = Proxy<IReceiveEndpointConfigurator>(out _);
        ISagaConfigurator<DefinitionSaga> saga = Proxy<ISagaConfigurator<DefinitionSaga>>(out RecordingProxy sagaRecorder);
        IRegistrationContext context = Proxy<IRegistrationContext>(out _);
        sagaRecorder.Handler = (method, arguments) =>
        {
            Assert.Equal("set_ConcurrentMessageLimit", method.Name);
            Assert.Equal(7, Assert.IsType<int>(arguments![0]));
            definition.Events.Add("limit");
            return null;
        };
        ISagaDefinition<DefinitionSaga> contract = definition;

        AssertParameter("endpointConfigurator", () => contract.Configure(null!, null!, null!));
        AssertParameter("sagaConfigurator", () => contract.Configure(endpoint, null!, null!));
        AssertParameter("context", () => contract.Configure(endpoint, saga, null!));
        Assert.Empty(definition.Events);

        contract.Configure(endpoint, saga, context);

        Assert.Equal(["limit", "configure"], definition.Events);
        Assert.Same(endpoint, definition.EndpointConfigurator);
        Assert.Same(saga, definition.SagaConfigurator);
        Assert.Same(context, definition.Context);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DEFINITION", "concurrent-limit-null-positive-and-stable-invalid-parameter")]
    public void ConcurrentMessageLimit_AcceptsNullOrPositiveAndRejectsNonPositiveWithoutMutation()
    {
        var definition = new InspectableDefinition();

        definition.SetConcurrentMessageLimit(null);
        Assert.Null(definition.ConcurrentMessageLimit);
        definition.SetConcurrentMessageLimit(5);
        Assert.Equal(5, definition.ConcurrentMessageLimit);

        foreach (int invalid in new[] { 0, -1 })
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                definition.SetConcurrentMessageLimit(invalid));
            Assert.Equal(nameof(SagaDefinition<DefinitionSaga>.ConcurrentMessageLimit), exception.ParamName);
            Assert.Equal(invalid, Assert.IsType<int>(exception.ActualValue));
            Assert.Equal(5, definition.ConcurrentMessageLimit);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DEFINITION", "endpoint-name-explicit-priority-and-dynamic-derived-values")]
    public void EndpointName_GuardsFormatterAndKeepsDerivedNamesDynamicWhileExplicitNameWins()
    {
        var definition = new InspectableDefinition();
        IEndpointNameFormatter formatter = Proxy<IEndpointNameFormatter>(out RecordingProxy formatterRecorder);
        var fallbackNames = new Queue<string>(["fallback-one", "fallback-two"]);
        formatterRecorder.Handler = (_, _) => fallbackNames.Dequeue();
        ISagaDefinition contract = definition;

        Assert.Equal("fallback-one", contract.GetEndpointName(formatter));
        Assert.Equal("fallback-two", contract.GetEndpointName(formatter));

        IEndpointDefinition<DefinitionSaga> endpointDefinition =
            Proxy<IEndpointDefinition<DefinitionSaga>>(out RecordingProxy definitionRecorder);
        var definitionNames = new Queue<string>(["definition-one", "definition-two"]);
        definitionRecorder.Handler = (method, arguments) =>
        {
            Assert.Equal(nameof(IEndpointDefinition.GetEndpointName), method.Name);
            Assert.Same(formatter, arguments![0]);
            return definitionNames.Dequeue();
        };
        definition.EndpointDefinition = endpointDefinition;

        Assert.Equal("definition-one", contract.GetEndpointName(formatter));
        Assert.Equal("definition-two", contract.GetEndpointName(formatter));
        definition.SetEndpointName("explicit-name");
        Assert.Equal("explicit-name", contract.GetEndpointName(formatter));
        AssertParameter("formatter", () => contract.GetEndpointName(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DEFINITION", "endpoint-callback-commit-after-success-and-failure-isolation")]
    public void Endpoint_CommitsOnlyAfterASuccessfulCallbackAndPreservesThePreviousDefinitionOnFailure()
    {
        var definition = new InspectableDefinition();
        IEndpointDefinition<DefinitionSaga> original = Proxy<IEndpointDefinition<DefinitionSaga>>(out _);
        definition.EndpointDefinition = original;
        var failure = new InvalidOperationException("endpoint callback failed");
        IEndpointRegistrationConfigurator? failedConfigurator = null;

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() =>
            definition.ConfigureEndpoint(configurator =>
            {
                failedConfigurator = configurator;
                Assert.Same(original, definition.EndpointDefinition);
                throw failure;
            }));

        Assert.Same(failure, thrown);
        Assert.NotNull(failedConfigurator);
        Assert.Same(original, definition.EndpointDefinition);

        IEndpointRegistrationConfigurator? successfulConfigurator = null;
        definition.ConfigureEndpoint(configurator =>
        {
            successfulConfigurator = configurator;
            Assert.Same(original, definition.EndpointDefinition);
        });

        Assert.NotNull(successfulConfigurator);
        Assert.NotSame(original, definition.EndpointDefinition);
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

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (Handler != null)
                return Handler(targetMethod, args);

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }

    private sealed class InspectableDefinition : SagaDefinition<DefinitionSaga>
    {
        public List<string> Events { get; } = [];

        public IReceiveEndpointConfigurator? EndpointConfigurator { get; private set; }

        public ISagaConfigurator<DefinitionSaga>? SagaConfigurator { get; private set; }

        public IRegistrationContext? Context { get; private set; }

        public void SetConcurrentMessageLimit(int? value) => ConcurrentMessageLimit = value;

        public void SetEndpointName(string value) => EndpointName = value;

        public void ConfigureEndpoint(Action<IEndpointRegistrationConfigurator>? configure) => Endpoint(configure);

        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<DefinitionSaga> sagaConfigurator,
            IRegistrationContext context)
        {
            Events.Add("configure");
            EndpointConfigurator = endpointConfigurator;
            SagaConfigurator = sagaConfigurator;
            Context = context;
        }
    }

    public sealed class DefinitionSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
