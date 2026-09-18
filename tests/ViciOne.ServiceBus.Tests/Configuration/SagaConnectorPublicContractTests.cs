using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaConnectorPublicContractTests
{
    private const BindingFlags DeclaredPublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-PUBLIC-CONTRACT", "type-accessibility-kind-and-generic-arity")]
    public void ConnectorContracts_AreExactlyPublicTopLevelInterfacesWithExpectedArities()
    {
        (Type Contract, int Arity)[] contracts =
        [
            (typeof(ISagaConnectorFactory), 0),
            (typeof(ISagaConnector), 0),
            (typeof(ISagaMessageConnector), 0),
            (typeof(ISagaMessageConnector<>), 1),
            (typeof(ISagaConnectorCache), 0),
        ];

        foreach ((Type contract, int arity) in contracts)
        {
            AssertPublicInterface(contract);
            Assert.Equal(arity, contract.GetGenericArguments().Length);
            Assert.Equal(arity > 0, contract.IsGenericTypeDefinition);
            Assert.Empty(contract.GetConstructors(DeclaredPublicMembers));
            Assert.Empty(contract.GetNestedTypes(BindingFlags.Public));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-PUBLIC-CONTRACT", "factory-generic-method-constraint-and-result")]
    public void SagaConnectorFactory_HasOneExactGenericCreationMethod()
    {
        Type factory = typeof(ISagaConnectorFactory);
        AssertDirectInterfaces(factory);
        AssertNoDeclaredDataMembers(factory);

        MethodInfo method = Assert.Single(DeclaredMethods(factory));
        Assert.Equal(nameof(ISagaConnectorFactory.CreateMessageConnector), method.Name);
        AssertInterfaceMethod(method, factory);
        Assert.True(method.IsGenericMethodDefinition);
        Type saga = AssertSingleGenericParameter(method, "T");
        AssertSagaParameter(saga, "T");
        Assert.Empty(method.GetParameters());
        Assert.Equal(typeof(ISagaMessageConnector<>).MakeGenericType(saga), method.ReturnType);
        AssertReturnNotNull(method);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-PUBLIC-CONTRACT", "connector-generic-methods-parameters-and-results")]
    public void SagaConnector_HasExactGenericSpecificationAndConnectionMethods()
    {
        Type connector = typeof(ISagaConnector);
        AssertDirectInterfaces(connector);
        AssertNoDeclaredDataMembers(connector);

        MethodInfo[] methods = DeclaredMethods(connector);
        Assert.Equal(2, methods.Length);

        MethodInfo create = Assert.Single(methods, method =>
            method.Name == nameof(ISagaConnector.CreateSagaSpecification));
        AssertInterfaceMethod(create, connector);
        Assert.True(create.IsGenericMethodDefinition);
        Type createSaga = AssertSingleGenericParameter(create, "T");
        AssertSagaParameter(createSaga, "T");
        Assert.Empty(create.GetParameters());
        Assert.Equal(typeof(ISagaSpecification<>).MakeGenericType(createSaga), create.ReturnType);
        AssertReturnNotNull(create);

        MethodInfo connect = Assert.Single(methods, method =>
            method.Name == nameof(ISagaConnector.ConnectSaga));
        AssertInterfaceMethod(connect, connector);
        Assert.True(connect.IsGenericMethodDefinition);
        Type connectSaga = AssertSingleGenericParameter(connect, "T");
        AssertSagaParameter(connectSaga, "T");
        Assert.Equal(typeof(ConnectHandle), connect.ReturnType);
        AssertReturnNotNull(connect);
        AssertParameters(
            connect,
            ("consumePipe", typeof(IConsumePipeConnector)),
            ("repository", typeof(ISagaRepository<>).MakeGenericType(connectSaga)),
            ("specification", typeof(ISagaSpecification<>).MakeGenericType(connectSaga)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-PUBLIC-CONTRACT", "message-connector-layering-members-and-nullability")]
    public void SagaMessageConnectorContracts_HaveExactLayeringMembersAndNullability()
    {
        Type untyped = typeof(ISagaMessageConnector);
        AssertDirectInterfaces(untyped);
        Assert.Empty(DeclaredMethods(untyped));
        Assert.Empty(untyped.GetEvents(DeclaredPublicMembers));
        Assert.Empty(untyped.GetFields(DeclaredPublicMembers));

        PropertyInfo messageType = Assert.Single(untyped.GetProperties(DeclaredPublicMembers));
        AssertReadOnlyInterfaceProperty(messageType, untyped, nameof(ISagaMessageConnector.MessageType), typeof(Type));

        Type typed = typeof(ISagaMessageConnector<>);
        Type saga = Assert.Single(typed.GetGenericArguments());
        AssertSagaParameter(saga, "TSaga");
        AssertDirectInterfaces(typed, untyped);
        AssertNoDeclaredDataMembers(typed);

        MethodInfo[] methods = DeclaredMethods(typed);
        Assert.Equal(2, methods.Length);

        MethodInfo create = Assert.Single(methods, method =>
            method.Name == nameof(ISagaMessageConnector<ContractSaga>.CreateSagaMessageSpecification));
        AssertInterfaceMethod(create, typed);
        Assert.False(create.IsGenericMethod);
        Assert.Empty(create.GetParameters());
        Assert.Equal(typeof(ISagaMessageSpecification<>).MakeGenericType(saga), create.ReturnType);
        AssertReturnNotNull(create);

        MethodInfo connect = Assert.Single(methods, method =>
            method.Name == nameof(ISagaMessageConnector<ContractSaga>.ConnectSaga));
        AssertInterfaceMethod(connect, typed);
        Assert.False(connect.IsGenericMethod);
        Assert.Equal(typeof(ConnectHandle), connect.ReturnType);
        AssertReturnNotNull(connect);
        AssertParameters(
            connect,
            ("consumePipe", typeof(IConsumePipeConnector)),
            ("repository", typeof(ISagaRepository<>).MakeGenericType(saga)),
            ("specification", typeof(ISagaSpecification<>).MakeGenericType(saga)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONNECTOR-PUBLIC-CONTRACT", "cache-readonly-connector-accessor")]
    public void SagaConnectorCache_HasOneExactNonNullableReadOnlyConnectorProperty()
    {
        Type cache = typeof(ISagaConnectorCache);
        AssertDirectInterfaces(cache);
        Assert.Empty(DeclaredMethods(cache));
        Assert.Empty(cache.GetEvents(DeclaredPublicMembers));
        Assert.Empty(cache.GetFields(DeclaredPublicMembers));

        PropertyInfo connector = Assert.Single(cache.GetProperties(DeclaredPublicMembers));
        AssertReadOnlyInterfaceProperty(
            connector,
            cache,
            nameof(ISagaConnectorCache.Connector),
            typeof(ISagaConnector));
    }

    private static void AssertPublicInterface(Type type)
    {
        Assert.Equal("ViciOne.ServiceBus.Configuration", type.Namespace);
        Assert.True(type.IsPublic);
        Assert.True(type.IsVisible);
        Assert.True(type.IsInterface);
        Assert.True(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.False(type.IsNested);
        Assert.Null(type.BaseType);
    }

    private static Type AssertSingleGenericParameter(MethodInfo method, string name)
    {
        Type parameter = Assert.Single(method.GetGenericArguments());
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

    private static void AssertInterfaceMethod(MethodInfo method, Type declaringType)
    {
        Assert.Equal(declaringType, method.DeclaringType);
        Assert.True(method.IsPublic);
        Assert.True(method.IsAbstract);
        Assert.True(method.IsVirtual);
        Assert.True(method.IsHideBySig);
        Assert.True((method.Attributes & MethodAttributes.NewSlot) != 0);
        Assert.False(method.IsFinal);
        Assert.False(method.IsStatic);
        Assert.False(method.IsSpecialName);
        Assert.Equal(method, method.GetBaseDefinition());
    }

    private static void AssertReadOnlyInterfaceProperty(
        PropertyInfo property,
        Type declaringType,
        string name,
        Type propertyType)
    {
        Assert.Equal(declaringType, property.DeclaringType);
        Assert.Equal(name, property.Name);
        Assert.Equal(propertyType, property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
        MethodInfo getter = property.GetMethod!;
        Assert.Equal(declaringType, getter.DeclaringType);
        Assert.True(getter.IsPublic);
        Assert.True(getter.IsAbstract);
        Assert.True(getter.IsVirtual);
        Assert.True(getter.IsHideBySig);
        Assert.True(getter.IsSpecialName);
        Assert.True((getter.Attributes & MethodAttributes.NewSlot) != 0);
        Assert.False(getter.IsFinal);
        Assert.False(getter.IsStatic);
        Assert.Empty(getter.GetParameters());
        Assert.Equal(propertyType, getter.ReturnType);
        Assert.Equal(getter, getter.GetBaseDefinition());
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(property).ReadState);
    }

    private static void AssertParameters(MethodInfo method, params (string Name, Type Type)[] expected)
    {
        ParameterInfo[] actual = method.GetParameters();
        Assert.Equal(expected.Length, actual.Length);

        for (var index = 0; index < expected.Length; index++)
        {
            ParameterInfo parameter = actual[index];
            Assert.Equal(index, parameter.Position);
            Assert.Equal(expected[index].Name, parameter.Name);
            Assert.Equal(expected[index].Type, parameter.ParameterType);
            Assert.False(parameter.IsIn);
            Assert.False(parameter.IsOut);
            Assert.False(parameter.IsOptional);
            Assert.False(parameter.HasDefaultValue);
            Assert.False(parameter.ParameterType.IsByRef);
            Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameter).ReadState);
        }
    }

    private static void AssertReturnNotNull(MethodInfo method)
    {
        Assert.False(method.ReturnType.IsByRef);
        Assert.False(method.ReturnType.IsPointer);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(method.ReturnParameter).ReadState);
    }

    private static void AssertNoDeclaredDataMembers(Type type)
    {
        Assert.Empty(type.GetProperties(DeclaredPublicMembers));
        Assert.Empty(type.GetEvents(DeclaredPublicMembers));
        Assert.Empty(type.GetFields(DeclaredPublicMembers));
    }

    private static MethodInfo[] DeclaredMethods(Type type) =>
        type.GetMethods(DeclaredPublicMembers).Where(static method => !method.IsSpecialName).ToArray();

    private static string TypeIdentity(Type type) => type.ToString();

    private sealed class ContractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
