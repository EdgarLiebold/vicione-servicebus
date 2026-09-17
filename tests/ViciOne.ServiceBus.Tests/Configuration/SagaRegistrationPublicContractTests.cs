using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaRegistrationPublicContractTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    private const BindingFlags DeclaredPublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-PUBLIC-CONTRACT", "public-type-accessibility-and-kinds")]
    public void PublicTypes_HaveExactAccessibilityKindsAndGenericArities()
    {
        Type[] interfaces =
        [
            typeof(ISagaRegistration),
            typeof(ISagaRegistrationConfigurator),
            typeof(ISagaRegistrationConfigurator<>),
            typeof(ISagaRepositoryDecoratorRegistration<>),
            typeof(ISagaRepositoryRegistrationConfigurator<>),
            typeof(ISagaRepositoryRegistrationProvider),
        ];

        foreach (Type contract in interfaces)
        {
            AssertPublicTopLevelType(contract);
            Assert.True(contract.IsInterface);
            Assert.True(contract.IsAbstract);
            Assert.False(contract.IsSealed);
        }

        Assert.False(typeof(ISagaRegistration).IsGenericType);
        Assert.False(typeof(ISagaRegistrationConfigurator).IsGenericType);
        Assert.True(typeof(ISagaRegistrationConfigurator<>).IsGenericTypeDefinition);
        Assert.True(typeof(ISagaRepositoryDecoratorRegistration<>).IsGenericTypeDefinition);
        Assert.True(typeof(ISagaRepositoryRegistrationConfigurator<>).IsGenericTypeDefinition);
        Assert.False(typeof(ISagaRepositoryRegistrationProvider).IsGenericType);

        Type definition = typeof(DefaultSagaDefinition<>);
        AssertPublicTopLevelType(definition);
        Assert.True(definition.IsClass);
        Assert.False(definition.IsInterface);
        Assert.False(definition.IsAbstract);
        Assert.False(definition.IsSealed);
        Assert.True(definition.IsGenericTypeDefinition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-PUBLIC-CONTRACT", "default-definition-inheritance-constraint-and-surface")]
    public void DefaultSagaDefinition_HasOnlyThePublicConstructorAndInheritedDefinitionSurface()
    {
        Type definition = typeof(DefaultSagaDefinition<>);
        Type saga = AssertSingleGenericParameter(definition, "TSaga");
        AssertSagaParameter(saga, "TSaga");

        Type expectedBase = typeof(SagaDefinition<>).MakeGenericType(saga);
        Assert.Equal(expectedBase, definition.BaseType);
        Assert.Equal(
            expectedBase.GetInterfaces().OrderBy(TypeIdentity, StringComparer.Ordinal),
            definition.GetInterfaces().OrderBy(TypeIdentity, StringComparer.Ordinal));

        ConstructorInfo constructor = Assert.Single(definition.GetConstructors(DeclaredPublicInstance));
        Assert.True(constructor.IsPublic);
        Assert.Empty(constructor.GetParameters());
        Assert.Empty(DeclaredMethods(definition));
        Assert.Empty(definition.GetProperties(DeclaredPublicMembers));
        Assert.Empty(definition.GetEvents(DeclaredPublicMembers));
        Assert.Empty(definition.GetFields(DeclaredPublicMembers));

        var instance = new DefaultSagaDefinition<ContractSaga>();
        ISagaDefinition contract = instance;

        Assert.Equal(typeof(ContractSaga), contract.SagaType);
        Assert.Null(contract.EndpointDefinition);
        Assert.Null(instance.EndpointDefinition);
        Assert.Null(instance.ConcurrentMessageLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-PUBLIC-CONTRACT", "saga-registration-inheritance-members-and-nullability")]
    public void SagaRegistration_HasExactInheritanceMembersConstraintsAndNullability()
    {
        Type registration = typeof(ISagaRegistration);
        AssertDirectInterfaces(registration, typeof(IRegistration));
        Assert.Empty(registration.GetGenericArguments());

        PropertyInfo stateMachineType = Assert.Single(registration.GetProperties(DeclaredPublicMembers));
        Assert.Equal(nameof(ISagaRegistration.StateMachineType), stateMachineType.Name);
        Assert.Equal(typeof(Type), stateMachineType.PropertyType);
        Assert.NotNull(stateMachineType.GetMethod);
        AssertInterfaceAccessor(stateMachineType.GetMethod!, registration);
        Assert.Null(stateMachineType.SetMethod);
        Assert.Equal(NullabilityState.Nullable, Nullability.Create(stateMachineType).ReadState);

        MethodInfo[] methods = DeclaredMethods(registration);
        Assert.Equal(3, methods.Length);

        MethodInfo addConfigureAction = Assert.Single(methods, method =>
            method.Name == nameof(ISagaRegistration.AddConfigureAction)
            && method.IsGenericMethodDefinition);
        AssertInterfaceOperation(addConfigureAction, registration);
        Assert.Equal(typeof(void), addConfigureAction.ReturnType);
        Type state = AssertSingleGenericParameter(addConfigureAction, "T");
        AssertGenericParameter(state, "T", GenericParameterAttributes.ReferenceTypeConstraint);
        Type nullableCallback = typeof(Action<,>).MakeGenericType(
            typeof(IRegistrationContext),
            typeof(ISagaConfigurator<>).MakeGenericType(state));
        AssertParameterContract(addConfigureAction, [nullableCallback], ["configure"]);
        AssertCallbackNullability(addConfigureAction.GetParameters()[0], NullabilityState.Nullable);

        MethodInfo configure = AssertMethod(
            registration,
            nameof(ISagaRegistration.Configure),
            typeof(void),
            typeof(IReceiveEndpointConfigurator),
            typeof(IRegistrationContext));
        AssertParameterContract(configure,
            [typeof(IReceiveEndpointConfigurator), typeof(IRegistrationContext)],
            ["configurator", "context"]);
        AssertAllParametersNotNull(configure);

        MethodInfo getDefinition = AssertMethod(
            registration,
            nameof(ISagaRegistration.GetDefinition),
            typeof(ISagaDefinition),
            typeof(IRegistrationContext));
        AssertParameterContract(getDefinition, [typeof(IRegistrationContext)], ["context"]);
        AssertAllParametersNotNull(getDefinition);
        AssertReturnNotNull(getDefinition);

        Assert.Empty(registration.GetEvents(DeclaredPublicMembers));
        Assert.Empty(registration.GetFields(DeclaredPublicMembers));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-PUBLIC-CONTRACT", "configurator-base-and-new-typed-callback-shapes")]
    public void SagaRegistrationConfigurators_ExposeExactBaseAndNewTypedCallbackShapes()
    {
        Type untyped = typeof(ISagaRegistrationConfigurator);
        AssertDirectInterfaces(untyped);
        Assert.Empty(untyped.GetGenericArguments());
        Assert.Empty(untyped.GetProperties(DeclaredPublicMembers));

        MethodInfo[] untypedMethods = DeclaredMethods(untyped);
        Assert.Equal(2, untypedMethods.Length);
        Type endpointCallback = typeof(Action<IEndpointRegistrationConfigurator>);
        MethodInfo untypedEndpoint = AssertMethod(
            untyped,
            nameof(ISagaRegistrationConfigurator.Endpoint),
            untyped,
            endpointCallback);
        AssertParameterContract(untypedEndpoint, [endpointCallback], ["configure"]);
        AssertCallbackNullability(untypedEndpoint.GetParameters()[0], NullabilityState.NotNull);
        AssertReturnNotNull(untypedEndpoint);

        MethodInfo exclude = AssertMethod(
            untyped,
            nameof(ISagaRegistrationConfigurator.ExcludeFromConfigureEndpoints),
            typeof(void));
        Assert.Empty(exclude.GetParameters());

        Type typed = typeof(ISagaRegistrationConfigurator<>);
        Type saga = AssertSingleGenericParameter(typed, "TSaga");
        AssertSagaParameter(saga, "TSaga");
        AssertDirectInterfaces(typed, untyped);
        Assert.Empty(typed.GetProperties(DeclaredPublicMembers));

        MethodInfo[] typedMethods = DeclaredMethods(typed);
        Assert.Equal(2, typedMethods.Length);
        MethodInfo typedEndpoint = AssertMethod(
            typed,
            nameof(ISagaRegistrationConfigurator.Endpoint),
            typed,
            endpointCallback);
        AssertParameterContract(typedEndpoint, [endpointCallback], ["configure"]);
        AssertCallbackNullability(typedEndpoint.GetParameters()[0], NullabilityState.NotNull);
        AssertReturnNotNull(typedEndpoint);

        Type repositoryConfigurator = typeof(ISagaRepositoryRegistrationConfigurator<>).MakeGenericType(saga);
        Type repositoryCallback = typeof(Action<>).MakeGenericType(repositoryConfigurator);
        MethodInfo repository = AssertMethod(
            typed,
            nameof(ISagaRegistrationConfigurator<ContractSaga>.Repository),
            typed,
            repositoryCallback);
        AssertParameterContract(repository, [repositoryCallback], ["configure"]);
        AssertCallbackNullability(repository.GetParameters()[0], NullabilityState.NotNull);
        AssertReturnNotNull(repository);

        Assert.Equal(untypedEndpoint.Name, typedEndpoint.Name);
        Assert.Equal(
            untypedEndpoint.GetParameters().Select(static parameter => parameter.ParameterType),
            typedEndpoint.GetParameters().Select(static parameter => parameter.ParameterType));
        Assert.NotEqual(untypedEndpoint.ReturnType, typedEndpoint.ReturnType);
        Assert.Equal(untyped, untypedEndpoint.ReturnType);
        Assert.Equal(typed, typedEndpoint.ReturnType);

        AssertNoFieldsOrEvents(untyped);
        AssertNoFieldsOrEvents(typed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-PUBLIC-CONTRACT", "repository-decorator-and-service-collection-contracts")]
    public void RepositoryRegistrationContracts_HaveExactInheritanceConstraintsAndSynchronousShapes()
    {
        Type decorator = typeof(ISagaRepositoryDecoratorRegistration<>);
        Type decoratorSaga = AssertSingleGenericParameter(decorator, "TSaga");
        AssertSagaParameter(decoratorSaga, "TSaga");
        AssertDirectInterfaces(decorator);
        Assert.Empty(decorator.GetProperties(DeclaredPublicMembers));
        Assert.Single(DeclaredMethods(decorator));
        MethodInfo decorate = AssertMethod(
            decorator,
            nameof(ISagaRepositoryDecoratorRegistration<ContractSaga>.DecorateSagaRepository),
            typeof(ISagaRepository<>).MakeGenericType(decoratorSaga),
            typeof(ISagaRepository<>).MakeGenericType(decoratorSaga));
        AssertParameterContract(
            decorate,
            [typeof(ISagaRepository<>).MakeGenericType(decoratorSaga)],
            ["repository"]);
        AssertAllParametersNotNull(decorate);
        AssertReturnNotNull(decorate);
        AssertNoFieldsOrEvents(decorator);

        Type configurator = typeof(ISagaRepositoryRegistrationConfigurator<>);
        Type configuratorSaga = AssertSingleGenericParameter(configurator, "TSaga");
        AssertSagaParameter(configuratorSaga, "TSaga");
        AssertDirectInterfaces(configurator, typeof(IServiceCollection));
        Assert.Empty(DeclaredMethods(configurator));
        Assert.Empty(configurator.GetProperties(DeclaredPublicMembers));
        AssertNoFieldsOrEvents(configurator);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-PUBLIC-CONTRACT", "repository-provider-generic-callback-contract")]
    public void RepositoryRegistrationProvider_HasOneExactSynchronousGenericConfigurationMethod()
    {
        Type provider = typeof(ISagaRepositoryRegistrationProvider);
        AssertDirectInterfaces(provider);
        Assert.Empty(provider.GetGenericArguments());
        Assert.Empty(provider.GetProperties(DeclaredPublicMembers));

        MethodInfo configure = Assert.Single(DeclaredMethods(provider));
        Assert.Equal(nameof(ISagaRepositoryRegistrationProvider.Configure), configure.Name);
        AssertInterfaceOperation(configure, provider);
        Assert.True(configure.IsGenericMethodDefinition);
        Assert.Equal(typeof(void), configure.ReturnType);
        Type saga = AssertSingleGenericParameter(configure, "TSaga");
        AssertSagaParameter(saga, "TSaga");
        Type configurator = typeof(ISagaRegistrationConfigurator<>).MakeGenericType(saga);
        AssertParameterContract(configure, [configurator], ["configurator"]);
        AssertAllParametersNotNull(configure);
        AssertNoFieldsOrEvents(provider);
    }

    private static void AssertPublicTopLevelType(Type type)
    {
        Assert.Equal("ViciOne.ServiceBus.Configuration", type.Namespace);
        Assert.True(type.IsPublic);
        Assert.True(type.IsVisible);
        Assert.False(type.IsNested);
    }

    private static Type AssertSingleGenericParameter(Type owner, string name)
    {
        Type parameter = Assert.Single(owner.GetGenericArguments());
        Assert.Equal(name, parameter.Name);
        return parameter;
    }

    private static Type AssertSingleGenericParameter(MethodInfo owner, string name)
    {
        Type parameter = Assert.Single(owner.GetGenericArguments());
        Assert.Equal(name, parameter.Name);
        return parameter;
    }

    private static void AssertSagaParameter(Type parameter, string name) =>
        AssertGenericParameter(
            parameter,
            name,
            GenericParameterAttributes.ReferenceTypeConstraint,
            typeof(ISaga));

    private static void AssertGenericParameter(
        Type parameter,
        string name,
        GenericParameterAttributes attributes,
        params Type[] constraints)
    {
        const GenericParameterAttributes relevantAttributes =
            GenericParameterAttributes.VarianceMask | GenericParameterAttributes.SpecialConstraintMask;

        Assert.True(parameter.IsGenericParameter);
        Assert.Equal(name, parameter.Name);
        Assert.Equal(attributes, parameter.GenericParameterAttributes & relevantAttributes);
        Assert.Equal(
            constraints.OrderBy(TypeIdentity, StringComparer.Ordinal),
            parameter.GetGenericParameterConstraints().OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static void AssertDirectInterfaces(Type type, params Type[] expected)
    {
        Type[] all = type.GetInterfaces();
        Type[] direct = all.Where(candidate => !all.Any(
            other => other != candidate && other.GetInterfaces().Contains(candidate))).ToArray();
        Assert.Equal(
            expected.OrderBy(TypeIdentity, StringComparer.Ordinal),
            direct.OrderBy(TypeIdentity, StringComparer.Ordinal));
    }

    private static MethodInfo AssertMethod(Type owner, string name, Type returnType, params Type[] parameterTypes)
    {
        MethodInfo method = Assert.Single(
            DeclaredMethods(owner),
            candidate => candidate.Name == name
                && !candidate.IsGenericMethodDefinition
                && candidate.GetParameters().Select(static parameter => parameter.ParameterType)
                    .SequenceEqual(parameterTypes));
        AssertInterfaceOperation(method, owner);
        Assert.Equal(returnType, method.ReturnType);
        return method;
    }

    private static void AssertInterfaceOperation(MethodInfo method, Type declaringType)
    {
        Assert.Equal(declaringType, method.DeclaringType);
        Assert.True(method.IsPublic);
        Assert.True(method.IsAbstract);
        Assert.True(method.IsVirtual);
        Assert.False(method.IsFinal);
        Assert.False(method.IsStatic);
    }

    private static void AssertInterfaceAccessor(MethodInfo accessor, Type declaringType)
    {
        Assert.True(accessor.IsSpecialName);
        AssertInterfaceOperation(accessor, declaringType);
    }

    private static void AssertParameterContract(MethodInfo method, Type[] types, string[] names)
    {
        ParameterInfo[] parameters = method.GetParameters();
        Assert.Equal(types, parameters.Select(static parameter => parameter.ParameterType));
        Assert.Equal(names, parameters.Select(static parameter => parameter.Name));
        Assert.All(parameters, parameter =>
        {
            Assert.False(parameter.IsOptional);
            Assert.False(parameter.HasDefaultValue);
            Assert.False(parameter.IsOut);
            Assert.False(parameter.ParameterType.IsByRef);
        });
    }

    private static void AssertCallbackNullability(ParameterInfo parameter, NullabilityState expectedState)
    {
        NullabilityInfo callback = Nullability.Create(parameter);
        Assert.Equal(expectedState, callback.ReadState);
        Assert.NotEmpty(callback.GenericTypeArguments);
        Assert.All(callback.GenericTypeArguments,
            argument => Assert.Equal(NullabilityState.NotNull, argument.ReadState));
    }

    private static void AssertAllParametersNotNull(MethodInfo method)
    {
        foreach (ParameterInfo parameter in method.GetParameters())
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameter).ReadState);
    }

    private static void AssertReturnNotNull(MethodInfo method) =>
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);

    private static void AssertNoFieldsOrEvents(Type type)
    {
        Assert.Empty(type.GetFields(DeclaredPublicMembers));
        Assert.Empty(type.GetEvents(DeclaredPublicMembers));
    }

    private static MethodInfo[] DeclaredMethods(Type type) =>
        type.GetMethods(DeclaredPublicMembers).Where(static method => !method.IsSpecialName).ToArray();

    private static string TypeIdentity(Type type) => type.ToString();

    private sealed class ContractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
