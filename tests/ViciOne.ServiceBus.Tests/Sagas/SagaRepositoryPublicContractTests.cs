using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaRepositoryPublicContractTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PUBLIC-CONTRACT", "capability-interface-inheritance-and-constraints")]
    public void RepositoryCapabilityContracts_HaveExactInheritanceAndGenericConstraints()
    {
        Type repository = typeof(ISagaRepository<>);
        Type repositorySaga = AssertSingleGenericParameter(repository, "TSaga");
        AssertDirectInterfaces(repository, typeof(IProbeSite));
        AssertSagaParameter(repositorySaga, "TSaga");
        AssertDeclaredProperties(repository, 0);
        AssertDeclaredMethods(repository, 2);

        Type loadable = typeof(ILoadableSagaRepository<>);
        Type loadableSaga = AssertSingleGenericParameter(loadable, "TSaga");
        AssertDirectInterfaces(
            loadable,
            typeof(ISagaRepository<>).MakeGenericType(loadableSaga),
            typeof(ILoadSagaRepository<>).MakeGenericType(loadableSaga));
        AssertSagaParameter(loadableSaga, "TSaga");
        AssertDeclaredProperties(loadable, 0);
        AssertDeclaredMethods(loadable, 0);

        Type query = typeof(IQuerySagaRepository<>);
        Type querySaga = AssertSingleGenericParameter(query, "TSaga");
        AssertDirectInterfaces(query, typeof(IProbeSite));
        AssertSagaParameter(querySaga, "TSaga");
        AssertDeclaredProperties(query, 0);
        AssertDeclaredMethods(query, 1);

        Type queryable = typeof(IQueryableSagaRepository<>);
        Type queryableSaga = AssertSingleGenericParameter(queryable, "TSaga");
        AssertDirectInterfaces(
            queryable,
            typeof(ILoadableSagaRepository<>).MakeGenericType(queryableSaga),
            typeof(IQuerySagaRepository<>).MakeGenericType(queryableSaga));
        AssertSagaParameter(queryableSaga, "TSaga");
        AssertDeclaredProperties(queryable, 0);
        AssertDeclaredMethods(queryable, 0);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PUBLIC-CONTRACT", "repository-operation-task-shapes-and-default-cancellation")]
    public void RepositoryOperationContracts_HaveExactTaskShapesConstraintsAndCancellationDefaults()
    {
        Type repository = typeof(ISagaRepository<>);
        Type saga = repository.GetGenericArguments()[0];

        MethodInfo send = AssertGenericMethod(repository, "SendAsync", 1, 3);
        Type sendMessage = AssertSingleGenericParameter(send, "T");
        AssertReferenceTypeParameter(sendMessage, "T");
        AssertMethodShape(
            send,
            typeof(Task),
            typeof(ConsumeContext<>).MakeGenericType(sendMessage),
            typeof(ISagaPolicy<,>).MakeGenericType(saga, sendMessage),
            typeof(IPipe<>).MakeGenericType(typeof(SagaConsumeContext<,>).MakeGenericType(saga, sendMessage)));
        AssertParameterNames(send, "context", "policy", "next");

        MethodInfo sendQuery = AssertGenericMethod(repository, "SendQueryAsync", 1, 4);
        Type queryMessage = AssertSingleGenericParameter(sendQuery, "T");
        AssertReferenceTypeParameter(queryMessage, "T");
        AssertMethodShape(
            sendQuery,
            typeof(Task),
            typeof(ConsumeContext<>).MakeGenericType(queryMessage),
            typeof(ISagaQuery<>).MakeGenericType(saga),
            typeof(ISagaPolicy<,>).MakeGenericType(saga, queryMessage),
            typeof(IPipe<>).MakeGenericType(typeof(SagaConsumeContext<,>).MakeGenericType(saga, queryMessage)));
        AssertParameterNames(sendQuery, "context", "query", "policy", "next");

        Type queryRepository = typeof(IQuerySagaRepository<>);
        Type querySaga = queryRepository.GetGenericArguments()[0];
        MethodInfo find = AssertMethod(
            queryRepository,
            "FindAsync",
            typeof(Task<>).MakeGenericType(typeof(IEnumerable<Guid>)),
            typeof(ISagaQuery<>).MakeGenericType(querySaga),
            typeof(CancellationToken));
        Assert.EndsWith("Async", find.Name, StringComparison.Ordinal);
        AssertParameterNames(find, "query", "cancellationToken");
        AssertCancellationTokenDefault(find);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PUBLIC-CONTRACT", "query-factory-variance-constraints-and-out-nullability")]
    public void SagaQueryFactoryContract_HasExactVarianceConstraintsAndTruthfulOutNullability()
    {
        Type factory = typeof(ISagaQueryFactory<,>);
        Type[] genericArguments = factory.GetGenericArguments();
        Type saga = genericArguments[0];
        Type message = genericArguments[1];
        AssertDirectInterfaces(factory, typeof(IProbeSite));
        AssertSagaParameter(saga, "TSaga");
        AssertGenericParameter(
            message,
            "TMessage",
            GenericParameterAttributes.Contravariant | GenericParameterAttributes.ReferenceTypeConstraint);
        AssertDeclaredProperties(factory, 0);
        AssertDeclaredMethods(factory, 1);

        MethodInfo method = AssertMethod(
            factory,
            "TryCreateQuery",
            typeof(bool),
            typeof(ConsumeContext<>).MakeGenericType(message),
            typeof(ISagaQuery<>).MakeGenericType(saga).MakeByRefType());
        AssertParameterNames(method, "context", "query");
        ParameterInfo output = method.GetParameters()[1];
        Assert.True(output.IsOut);
        Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(output).WriteState);
        NotNullWhenAttribute attribute = Assert.Single(output.GetCustomAttributes<NotNullWhenAttribute>());
        Assert.True(attribute.ReturnValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-REPOSITORY-PUBLIC-CONTRACT", "missing-instance-redelivery-interface-shape")]
    public void MissingInstanceRedeliveryContracts_HaveExactInheritancePropertyAndCallback()
    {
        Type configurator = typeof(IMissingInstanceRedeliveryConfigurator);
        AssertDirectInterfaces(configurator, typeof(IRedeliveryConfigurator));
        AssertDeclaredProperties(configurator, 1);
        PropertyInfo scheduler = Assert.IsAssignableFrom<PropertyInfo>(
            configurator.GetProperty("ConfigureMessageScheduler", DeclaredPublicInstance));
        Assert.Equal(typeof(bool), scheduler.PropertyType);
        Assert.False(scheduler.CanRead);
        Assert.Null(scheduler.GetMethod);
        Assert.True(scheduler.CanWrite);
        Assert.NotNull(scheduler.SetMethod);
        Assert.True(scheduler.SetMethod!.IsPublic);
        AssertDeclaredMethods(configurator, 0);

        Type typedConfigurator = typeof(IMissingInstanceRedeliveryConfigurator<,>);
        Type[] genericArguments = typedConfigurator.GetGenericArguments();
        Type instance = genericArguments[0];
        Type data = genericArguments[1];
        AssertDirectInterfaces(typedConfigurator, configurator);
        AssertGenericParameter(instance, "TInstance", GenericParameterAttributes.None, typeof(ISagaStateMachineInstance));
        AssertReferenceTypeParameter(data, "TData");
        AssertDeclaredProperties(typedConfigurator, 0);
        AssertDeclaredMethods(typedConfigurator, 1);

        Type callback = typeof(Func<,>).MakeGenericType(
            typeof(IMissingInstanceConfigurator<,>).MakeGenericType(instance, data),
            typeof(IPipe<>).MakeGenericType(typeof(ConsumeContext<>).MakeGenericType(data)));
        MethodInfo method = AssertMethod(
            typedConfigurator,
            "OnRedeliveryLimitReached",
            typeof(void),
            callback);
        AssertParameterNames(method, "configure");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-EXPRESSION-PROPERTY", "extension-overload-shapes-and-nullable-out-contracts")]
    public void PropertyValueExtractionContract_HasExactExtensionShapesAndNullableOuts()
    {
        Type extensions = typeof(SagaQueryExpressionPropertyExtensions);
        Assert.True(extensions.IsPublic);
        Assert.True(extensions.IsAbstract);
        Assert.True(extensions.IsSealed);

        MethodInfo[] methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        Assert.Equal(2, methods.Length);

        MethodInfo untyped = Assert.Single(methods, method => method.GetGenericArguments().Length == 1);
        Type untypedSaga = AssertSingleGenericParameter(untyped, "T");
        AssertSagaParameter(untypedSaga, "T");
        Assert.Equal("TryGetPropertyValue", untyped.Name);
        Assert.Equal(typeof(bool), untyped.ReturnType);
        AssertParameterTypes(
            untyped,
            typeof(ISagaQuery<>).MakeGenericType(untypedSaga),
            typeof(object).MakeByRefType());
        AssertParameterNames(untyped, "query", "value");
        AssertExtensionReceiver(untyped);
        AssertNullableOutputWithoutTruthinessPromise(untyped.GetParameters()[1]);

        MethodInfo typed = Assert.Single(methods, method => method.GetGenericArguments().Length == 2);
        Type[] typedArguments = typed.GetGenericArguments();
        Type typedSaga = typedArguments[0];
        Type property = typedArguments[1];
        AssertSagaParameter(typedSaga, "T");
        AssertGenericParameter(property, "TProperty", GenericParameterAttributes.None);
        Assert.Equal("TryGetPropertyValue", typed.Name);
        Assert.Equal(typeof(bool), typed.ReturnType);
        AssertParameterTypes(
            typed,
            typeof(ISagaQuery<>).MakeGenericType(typedSaga),
            property.MakeByRefType());
        AssertParameterNames(typed, "query", "value");
        AssertExtensionReceiver(typed);
        AssertNullableOutputWithoutTruthinessPromise(typed.GetParameters()[1]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-EXPRESSION-PROPERTY", "both-property-expression-success-and-false-paths")]
    public void PropertyValueExtraction_ReturnsExactValuesAndFalseDefaultsThroughBothOverloads()
    {
        ISagaQuery<RepositoryContractSaga> query = CreatePropertyQuery("order-42");

        Assert.True(query.TryGetPropertyValue(out object? untyped));
        Assert.Equal("order-42", untyped);
        Assert.True(query.TryGetPropertyValue<RepositoryContractSaga, string>(out string? typed));
        Assert.Equal("order-42", typed);

        ISagaQuery<RepositoryContractSaga> nullValueQuery = CreatePropertyQuery<string?>(null);
        Assert.True(nullValueQuery.TryGetPropertyValue(out object? nullUntyped));
        Assert.Null(nullUntyped);
        Assert.True(nullValueQuery.TryGetPropertyValue<RepositoryContractSaga, string?>(out string? nullTyped));
        Assert.Null(nullTyped);

        var ordinaryQuery = new SagaQuery<RepositoryContractSaga>(saga => saga.Name == "literal");
        Assert.False(ordinaryQuery.TryGetPropertyValue(out object? missingUntyped));
        Assert.Null(missingUntyped);
        Assert.False(ordinaryQuery.TryGetPropertyValue<RepositoryContractSaga, string>(out string? missingTyped));
        Assert.Null(missingTyped);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-EXPRESSION-PROPERTY", "null-and-malformed-query-boundaries")]
    public void PropertyValueExtraction_RejectsNullAndMalformedQueriesAtBothBoundaries()
    {
        ISagaQuery<RepositoryContractSaga> nullQuery = null!;
        ArgumentNullException untypedNull = Assert.Throws<ArgumentNullException>(
            () => nullQuery.TryGetPropertyValue(out object? _));
        Assert.Equal("query", untypedNull.ParamName);
        ArgumentNullException typedNull = Assert.Throws<ArgumentNullException>(
            () => nullQuery.TryGetPropertyValue<RepositoryContractSaga, string>(out _));
        Assert.Equal("query", typedNull.ParamName);

        var malformed = new NullExpressionQuery();
        ArgumentException untypedMalformed = Assert.Throws<ArgumentException>(
            () => malformed.TryGetPropertyValue(out object? _));
        Assert.Equal("query", untypedMalformed.ParamName);
        Assert.Contains("lambda expression", untypedMalformed.Message, StringComparison.Ordinal);
        ArgumentException typedMalformed = Assert.Throws<ArgumentException>(
            () => malformed.TryGetPropertyValue<RepositoryContractSaga, string>(out _));
        Assert.Equal("query", typedMalformed.ParamName);
        Assert.Contains("lambda expression", typedMalformed.Message, StringComparison.Ordinal);
    }

    private static ISagaQuery<RepositoryContractSaga> CreatePropertyQuery<TProperty>(TProperty value)
    {
        var owner = new PropertyExpressionPropertyValue<TProperty> { Value = value };
        ParameterExpression saga = Expression.Parameter(typeof(RepositoryContractSaga), "saga");
        MemberExpression sagaProperty = Expression.Property(saga, nameof(RepositoryContractSaga.Name));
        MemberExpression valueProperty = Expression.Property(
            Expression.Constant(owner),
            nameof(PropertyExpressionPropertyValue<TProperty>.Value));
        BinaryExpression equality = Expression.Equal(sagaProperty, valueProperty);
        var filter = Expression.Lambda<Func<RepositoryContractSaga, bool>>(equality, saga);
        return new SagaQuery<RepositoryContractSaga>(filter);
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

    private static void AssertReferenceTypeParameter(Type parameter, string name) =>
        AssertGenericParameter(parameter, name, GenericParameterAttributes.ReferenceTypeConstraint);

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

    private static void AssertDeclaredProperties(Type type, int expectedCount) =>
        Assert.Equal(expectedCount, type.GetProperties(DeclaredPublicInstance).Length);

    private static void AssertDeclaredMethods(Type type, int expectedCount) =>
        Assert.Equal(expectedCount, DeclaredMethods(type).Length);

    private static MethodInfo AssertMethod(Type owner, string name, Type returnType, params Type[] parameterTypes)
    {
        MethodInfo method = Assert.Single(
            DeclaredMethods(owner),
            candidate => candidate.Name == name
                && !candidate.IsGenericMethodDefinition
                && candidate.GetParameters().Select(static parameter => parameter.ParameterType)
                    .SequenceEqual(parameterTypes));
        AssertMethodShape(method, returnType, parameterTypes);
        return method;
    }

    private static MethodInfo AssertGenericMethod(Type owner, string name, int genericArity, int parameterCount)
    {
        MethodInfo method = Assert.Single(
            DeclaredMethods(owner),
            candidate => candidate.Name == name
                && candidate.IsGenericMethodDefinition
                && candidate.GetGenericArguments().Length == genericArity
                && candidate.GetParameters().Length == parameterCount);
        Assert.Equal(owner, method.DeclaringType);
        Assert.EndsWith("Async", method.Name, StringComparison.Ordinal);
        return method;
    }

    private static void AssertMethodShape(MethodInfo method, Type returnType, params Type[] parameterTypes)
    {
        Assert.NotNull(method.DeclaringType);
        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
        Assert.Equal(returnType, method.ReturnType);
        AssertParameterTypes(method, parameterTypes);
    }

    private static void AssertParameterTypes(MethodInfo method, params Type[] parameterTypes) =>
        Assert.Equal(parameterTypes, method.GetParameters().Select(static parameter => parameter.ParameterType));

    private static void AssertParameterNames(MethodInfo method, params string[] names) =>
        Assert.Equal(names, method.GetParameters().Select(static parameter => parameter.Name));

    private static void AssertCancellationTokenDefault(MethodInfo method)
    {
        ParameterInfo cancellationToken = Assert.Single(
            method.GetParameters(),
            static parameter => parameter.ParameterType == typeof(CancellationToken));
        Assert.Equal(method.GetParameters().Length - 1, cancellationToken.Position);
        Assert.True(cancellationToken.IsOptional);
        Assert.True(cancellationToken.HasDefaultValue);
        Assert.Null(cancellationToken.DefaultValue);
    }

    private static void AssertExtensionReceiver(MethodInfo method)
    {
        Assert.True(method.IsStatic);
        Assert.NotNull(method.GetCustomAttribute<ExtensionAttribute>());
    }

    private static void AssertNullableOutputWithoutTruthinessPromise(ParameterInfo parameter)
    {
        Assert.True(parameter.IsOut);
        Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(parameter).WriteState);
        Assert.Empty(parameter.GetCustomAttributes<NotNullWhenAttribute>());
    }

    private static MethodInfo[] DeclaredMethods(Type type) =>
        type.GetMethods(DeclaredPublicInstance).Where(static method => !method.IsSpecialName).ToArray();

    private static string TypeIdentity(Type type) => type.ToString();

    private sealed class RepositoryContractSaga : ISaga
    {
        public Guid CorrelationId { get; set; }

        public string? Name { get; set; }
    }

    private sealed class NullExpressionQuery : ISagaQuery<RepositoryContractSaga>
    {
        public Expression<Func<RepositoryContractSaga, bool>> FilterExpression => null!;

        public Func<RepositoryContractSaga, bool> GetFilter() => throw new NotSupportedException();
    }
}
