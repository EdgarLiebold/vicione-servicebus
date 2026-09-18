using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SagaQueryPropertySelectorDeepContractTests
{
    private const BindingFlags DeclaredPublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-PROPERTY-SELECTOR", "selector-interface-variance-member-and-nullability")]
    public void SelectorInterface_HasExactVarianceConstraintsMemberAndConditionalOutputContract()
    {
        Type selector = typeof(ISagaQueryPropertySelector<,>);
        AssertPublicInterface(selector);
        AssertDirectInterfaces(selector);

        Type[] arguments = selector.GetGenericArguments();
        Assert.Equal(2, arguments.Length);
        Type data = arguments[0];
        Type property = arguments[1];
        AssertGenericParameter(
            data,
            "TData",
            GenericParameterAttributes.Contravariant | GenericParameterAttributes.ReferenceTypeConstraint);
        AssertGenericParameter(property, "TProperty", GenericParameterAttributes.None);

        Assert.Empty(selector.GetProperties(DeclaredPublicMembers));
        MethodInfo method = Assert.Single(DeclaredMethods(selector));
        Assert.Equal(nameof(ISagaQueryPropertySelector<QueryData, string>.TryGetProperty), method.Name);
        AssertInterfaceMethod(method, selector);
        Assert.Equal(typeof(bool), method.ReturnType);

        ParameterInfo[] parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        AssertParameter(parameters[0], "context", typeof(ConsumeContext<>).MakeGenericType(data));
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameters[0]).ReadState);
        AssertParameter(parameters[1], "property", property.MakeByRefType());
        Assert.True(parameters[1].IsOut);
        Assert.Equal(NullabilityState.Nullable, Nullability.Create(parameters[1]).WriteState);
        NotNullWhenAttribute conditional = Assert.Single(parameters[1].GetCustomAttributes<NotNullWhenAttribute>());
        Assert.True(conditional.ReturnValue);
        AssertNoFieldsOrEvents(selector);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-PROPERTY-SELECTOR", "concrete-selector-surfaces-and-nullability")]
    public void ConcreteSelectors_HaveExactInheritanceConstraintsMembersAndNullability()
    {
        AssertHasValueSurface();
        AssertNotDefaultSurface();
        AssertReferenceSurface();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-PROPERTY-SELECTOR", "selector-constructor-null-admission")]
    public void Constructors_RejectNullSelectorAtTheOwnershipBoundary()
    {
        AssertArgument("selector", () =>
            new HasValueTypeSagaQueryPropertySelector<QueryData, ArbitraryValue>(null!));
        AssertArgument("selector", () =>
            new NotDefaultValueTypeSagaQueryPropertySelector<QueryData, ArbitraryValue>(null!));
        AssertArgument("selector", () =>
            new SagaQueryPropertySelector<QueryData, ReferenceResult>(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-PROPERTY-SELECTOR", "context-guard-precedes-callback")]
    public void TryGetProperty_RejectsNullContextBeforeInvokingAnySelector()
    {
        var hasValueInvocations = 0;
        var hasValue = new HasValueTypeSagaQueryPropertySelector<QueryData, int>(_ =>
        {
            hasValueInvocations++;
            return 1;
        });
        AssertArgument("context", () => hasValue.TryGetProperty(null!, out _));
        Assert.Equal(0, hasValueInvocations);

        var notDefaultInvocations = 0;
        var notDefault = new NotDefaultValueTypeSagaQueryPropertySelector<QueryData, int>(_ =>
        {
            notDefaultInvocations++;
            return 1;
        });
        AssertArgument("context", () => notDefault.TryGetProperty(null!, out _));
        Assert.Equal(0, notDefaultInvocations);

        var referenceInvocations = 0;
        var reference = new SagaQueryPropertySelector<QueryData, ReferenceResult>(_ =>
        {
            referenceInvocations++;
            return new ReferenceResult("unexpected");
        });
        AssertArgument("context", () => reference.TryGetProperty(null!, out _));
        Assert.Equal(0, referenceInvocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-PROPERTY-SELECTOR", "nullable-value-null-default-and-value-matrix")]
    public void HasValueSelector_AssignsAndClassifiesNullDefaultAndValueForBuiltInAndArbitraryStructs()
    {
        AssertHasValueResult<int>(null, false);
        AssertHasValueResult<int>(default(int), true);
        AssertHasValueResult<int>(47, true);

        AssertHasValueResult<ArbitraryValue>(null, false);
        AssertHasValueResult<ArbitraryValue>(default(ArbitraryValue), true);
        AssertHasValueResult<ArbitraryValue>(
            new ArbitraryValue(23, Guid.Parse("917f4cfb-dfea-419f-87b6-bb0969af90bb")),
            true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-PROPERTY-SELECTOR", "nondefault-value-built-in-and-arbitrary-struct-matrix")]
    public void NotDefaultSelector_AssignsAndClassifiesDefaultAndValueForBuiltInAndArbitraryStructs()
    {
        AssertNotDefaultResult(0, false);
        AssertNotDefaultResult(47, true);

        AssertNotDefaultResult(default(ArbitraryValue), false);
        AssertNotDefaultResult(new ArbitraryValue(23, Guid.Parse("eb5c6523-192d-4641-bac7-d32c0f22dbec")), true);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-QUERY-PROPERTY-SELECTOR", "reference-null-and-value-matrix")]
    public void ReferenceSelector_AssignsNullOrTheExactReferenceAndReportsAvailability()
    {
        AssertReferenceResult<ReferenceResult>(null, false);
        AssertReferenceResult(string.Empty, true);
        AssertReferenceResult(new ReferenceResult("selected"), true);
    }

    private static void AssertHasValueSurface()
    {
        Type selector = typeof(HasValueTypeSagaQueryPropertySelector<,>);
        Type[] arguments = AssertConcreteSelector(selector);
        Type data = arguments[0];
        Type property = arguments[1];
        AssertGenericParameter(data, "TData", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertStructParameter(property, "TProperty");

        Type nullableProperty = typeof(Nullable<>).MakeGenericType(property);
        Type context = typeof(ConsumeContext<>).MakeGenericType(data);
        Type callback = typeof(Func<,>).MakeGenericType(context, nullableProperty);
        AssertDirectInterfaces(
            selector,
            typeof(ISagaQueryPropertySelector<,>).MakeGenericType(data, nullableProperty));
        AssertConstructor(selector, callback, NullabilityState.Nullable);
        AssertTryGetProperty(selector, context, nullableProperty.MakeByRefType(), NullabilityState.Nullable, true);
    }

    private static void AssertNotDefaultSurface()
    {
        Type selector = typeof(NotDefaultValueTypeSagaQueryPropertySelector<,>);
        Type[] arguments = AssertConcreteSelector(selector);
        Type data = arguments[0];
        Type property = arguments[1];
        AssertGenericParameter(data, "TData", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertStructParameter(property, "TProperty");

        Type context = typeof(ConsumeContext<>).MakeGenericType(data);
        Type callback = typeof(Func<,>).MakeGenericType(context, property);
        AssertDirectInterfaces(
            selector,
            typeof(ISagaQueryPropertySelector<,>).MakeGenericType(data, property));
        AssertConstructor(selector, callback, NullabilityState.NotNull);
        AssertTryGetProperty(selector, context, property.MakeByRefType(), NullabilityState.NotNull, true);
    }

    private static void AssertReferenceSurface()
    {
        Type selector = typeof(SagaQueryPropertySelector<,>);
        Type[] arguments = AssertConcreteSelector(selector);
        Type data = arguments[0];
        Type property = arguments[1];
        AssertGenericParameter(data, "TData", GenericParameterAttributes.ReferenceTypeConstraint);
        AssertGenericParameter(property, "TProperty", GenericParameterAttributes.ReferenceTypeConstraint);

        Type context = typeof(ConsumeContext<>).MakeGenericType(data);
        Type callback = typeof(Func<,>).MakeGenericType(context, property);
        AssertDirectInterfaces(
            selector,
            typeof(ISagaQueryPropertySelector<,>).MakeGenericType(data, property));
        AssertConstructor(selector, callback, NullabilityState.Nullable);
        AssertTryGetProperty(selector, context, property.MakeByRefType(), NullabilityState.Nullable, true);
    }

    private static Type[] AssertConcreteSelector(Type selector)
    {
        Assert.Equal("ViciOne.ServiceBus.Configuration", selector.Namespace);
        Assert.True(selector.IsPublic);
        Assert.True(selector.IsVisible);
        Assert.True(selector.IsClass);
        Assert.False(selector.IsAbstract);
        Assert.False(selector.IsSealed);
        Assert.False(selector.IsNested);
        Assert.True(selector.IsGenericTypeDefinition);
        Assert.Equal(typeof(object), selector.BaseType);
        Assert.Empty(selector.GetProperties(DeclaredPublicMembers));
        AssertNoFieldsOrEvents(selector);

        Type[] arguments = selector.GetGenericArguments();
        Assert.Equal(2, arguments.Length);
        return arguments;
    }

    private static void AssertConstructor(Type selector, Type callbackType, NullabilityState callbackResultState)
    {
        ConstructorInfo constructor = Assert.Single(selector.GetConstructors(DeclaredPublicMembers));
        Assert.True(constructor.IsPublic);
        Assert.False(constructor.IsStatic);
        ParameterInfo parameter = Assert.Single(constructor.GetParameters());
        AssertParameter(parameter, "selector", callbackType);
        NullabilityInfo callback = Nullability.Create(parameter);
        Assert.Equal(NullabilityState.NotNull, callback.ReadState);
        Assert.Equal(2, callback.GenericTypeArguments.Length);
        Assert.Equal(NullabilityState.NotNull, callback.GenericTypeArguments[0].ReadState);
        Assert.Equal(callbackResultState, callback.GenericTypeArguments[1].ReadState);
    }

    private static void AssertTryGetProperty(
        Type selector,
        Type contextType,
        Type outputType,
        NullabilityState outputState,
        bool hasConditionalAnnotation)
    {
        MethodInfo method = Assert.Single(DeclaredMethods(selector));
        Assert.Equal("TryGetProperty", method.Name);
        Assert.Equal(selector, method.DeclaringType);
        Assert.True(method.IsPublic);
        Assert.False(method.IsAbstract);
        Assert.True(method.IsVirtual);
        Assert.True(method.IsFinal);
        Assert.False(method.IsStatic);
        Assert.Equal(typeof(bool), method.ReturnType);

        ParameterInfo[] parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        AssertParameter(parameters[0], "context", contextType);
        Assert.Equal(NullabilityState.NotNull, Nullability.Create(parameters[0]).ReadState);
        AssertParameter(parameters[1], "property", outputType);
        Assert.True(parameters[1].IsOut);
        Assert.Equal(outputState, Nullability.Create(parameters[1]).WriteState);
        NotNullWhenAttribute[] annotations = parameters[1].GetCustomAttributes<NotNullWhenAttribute>().ToArray();
        Assert.Equal(hasConditionalAnnotation ? 1 : 0, annotations.Length);
        if (hasConditionalAnnotation)
            Assert.True(annotations[0].ReturnValue);
    }

    private static void AssertHasValueResult<TProperty>(TProperty? selected, bool expectedResult)
        where TProperty : struct
    {
        ConsumeContext<QueryData> context = CreateContext();
        ConsumeContext<QueryData>? callbackContext = null;
        var invocations = 0;
        var selector = new HasValueTypeSagaQueryPropertySelector<QueryData, TProperty>(actualContext =>
        {
            invocations++;
            callbackContext = actualContext;
            return selected;
        });

        bool result = selector.TryGetProperty(context, out TProperty? output);

        Assert.Equal(expectedResult, result);
        Assert.Equal(selected, output);
        Assert.Equal(1, invocations);
        Assert.Same(context, callbackContext);
    }

    private static void AssertNotDefaultResult<TProperty>(TProperty selected, bool expectedResult)
        where TProperty : struct
    {
        ConsumeContext<QueryData> context = CreateContext();
        ConsumeContext<QueryData>? callbackContext = null;
        var invocations = 0;
        var selector = new NotDefaultValueTypeSagaQueryPropertySelector<QueryData, TProperty>(actualContext =>
        {
            invocations++;
            callbackContext = actualContext;
            return selected;
        });

        bool result = selector.TryGetProperty(context, out TProperty output);

        Assert.Equal(expectedResult, result);
        Assert.Equal(selected, output);
        Assert.Equal(1, invocations);
        Assert.Same(context, callbackContext);
    }

    private static void AssertReferenceResult<TProperty>(TProperty? selected, bool expectedResult)
        where TProperty : class
    {
        ConsumeContext<QueryData> context = CreateContext();
        ConsumeContext<QueryData>? callbackContext = null;
        var invocations = 0;
        var selector = new SagaQueryPropertySelector<QueryData, TProperty>(actualContext =>
        {
            invocations++;
            callbackContext = actualContext;
            return selected;
        });

        bool result = selector.TryGetProperty(context, out TProperty? output);

        Assert.Equal(expectedResult, result);
        Assert.Same(selected, output);
        Assert.Equal(1, invocations);
        Assert.Same(context, callbackContext);
    }

    private static ConsumeContext<QueryData> CreateContext() =>
        DispatchProxy.Create<ConsumeContext<QueryData>, PassiveProxy>();

    private static void AssertPublicInterface(Type type)
    {
        Assert.Equal("ViciOne.ServiceBus.Configuration", type.Namespace);
        Assert.True(type.IsPublic);
        Assert.True(type.IsVisible);
        Assert.True(type.IsInterface);
        Assert.True(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.False(type.IsNested);
        Assert.True(type.IsGenericTypeDefinition);
    }

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

    private static void AssertStructParameter(Type parameter, string name) =>
        AssertGenericParameter(
            parameter,
            name,
            GenericParameterAttributes.NotNullableValueTypeConstraint | GenericParameterAttributes.DefaultConstructorConstraint,
            typeof(ValueType));

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
        Assert.False(method.IsFinal);
        Assert.False(method.IsStatic);
    }

    private static void AssertParameter(ParameterInfo parameter, string name, Type type)
    {
        Assert.Equal(name, parameter.Name);
        Assert.Equal(type, parameter.ParameterType);
        Assert.False(parameter.IsOptional);
        Assert.False(parameter.HasDefaultValue);
    }

    private static void AssertNoFieldsOrEvents(Type type)
    {
        Assert.Empty(type.GetFields(DeclaredPublicMembers));
        Assert.Empty(type.GetEvents(DeclaredPublicMembers));
    }

    private static MethodInfo[] DeclaredMethods(Type type) =>
        type.GetMethods(DeclaredPublicMembers).Where(static method => !method.IsSpecialName).ToArray();

    private static void AssertArgument(string parameterName, Action action) =>
        Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

    private static string TypeIdentity(Type type) => type.ToString();

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The selector callback must not dereference the supplied context.");
    }

    private sealed class QueryData;

    private readonly record struct ArbitraryValue(int Number, Guid Token);

    private sealed record ReferenceResult(string Value);
}
