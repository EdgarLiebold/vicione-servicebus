using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Messages;
using ViciOne.ServiceBus.Courier.Serialization;
using ViciOne.ServiceBus.Serialization.Json.Converters;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierConventionSerializationDeepContractTests
{
    private static readonly Guid TrackingNumber = new("6ea4220b-4bf9-4bf0-b513-cc836a21debb");
    private static readonly Guid ExecutionId = new("3c0d67fb-9dc8-433f-ab05-67bf2598b490");

    private static readonly (Type Contract, Type Implementation)[] JsonMappings =
    [
        (typeof(IRoutingSlip), typeof(RoutingSlipRoutingSlip)),
        (typeof(IActivity), typeof(RoutingSlipActivity)),
        (typeof(IActivityLog), typeof(RoutingSlipActivityLog)),
        (typeof(ICompensateLog), typeof(RoutingSlipCompensateLog)),
        (typeof(IActivityException), typeof(RoutingSlipActivityException)),
        (typeof(ISubscription), typeof(RoutingSlipSubscription)),
        (typeof(IRoutingSlipCompleted), typeof(RoutingSlipCompletedMessage)),
        (typeof(IRoutingSlipFaulted), typeof(RoutingSlipFaultedMessage)),
        (typeof(IRoutingSlipActivityCompleted), typeof(RoutingSlipActivityCompletedMessage)),
        (typeof(IRoutingSlipActivityFaulted), typeof(RoutingSlipActivityFaultedMessage)),
        (typeof(IRoutingSlipActivityCompensated), typeof(RoutingSlipActivityCompensatedMessage)),
        (typeof(IRoutingSlipActivityCompensationFailed), typeof(RoutingSlipActivityCompensationFailedMessage)),
        (typeof(IRoutingSlipCompensationFailed), typeof(RoutingSlipCompensationFailedMessage)),
        (typeof(IRoutingSlipTerminated), typeof(RoutingSlipTerminatedMessage)),
        (typeof(IRoutingSlipRevised), typeof(RoutingSlipRevisedMessage)),
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONVENTIONS", "registration-api-is-internal-synchronous-and-json-only-is-module-initialized")]
    public void RegistrationApis_AreInternalSynchronousAndOnlyJsonMappingUsesModuleInitialization()
    {
        MethodInfo correlationRegister = AssertRegistrarShape(typeof(CourierCorrelationConventions));
        MethodInfo jsonRegister = AssertRegistrarShape(typeof(CourierJsonTypeMappings));

        Assert.Empty(correlationRegister.GetCustomAttributes<ModuleInitializerAttribute>());
        Assert.Single(jsonRegister.GetCustomAttributes<ModuleInitializerAttribute>());
        Assert.DoesNotContain("Async", correlationRegister.Name, StringComparison.Ordinal);
        Assert.DoesNotContain("Async", jsonRegister.Name, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CONVENTIONS", "concurrent-registration-is-idempotent-after-topology-freeze")]
    public void Registrars_AreIdempotentAndThreadSafeEvenAfterApplicationTopologyFreezes()
    {
        _ = GlobalTopology.Send;

        Parallel.For(0, 256, _ =>
        {
            CourierCorrelationConventions.Register();
            CourierJsonTypeMappings.Register();
        });

        IMessageCorrelationId<IRoutingSlip> before = GetCorrelationResolver<IRoutingSlip>();
        CourierCorrelationConventions.Register();
        CourierCorrelationConventions.Register();
        CourierJsonTypeMappings.Register();
        CourierJsonTypeMappings.Register();
        IMessageCorrelationId<IRoutingSlip> after = GetCorrelationResolver<IRoutingSlip>();

        Assert.Same(before, after);
        Assert.All(JsonMappings, mapping => AssertExactMapping(mapping.Contract, mapping.Implementation));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CORRELATION", "all-ten-courier-contracts-select-the-exact-identity-and-reject-empty-or-null")]
    public void CorrelationConventions_SelectExactTrackingOrExecutionIdentityForEveryContract()
    {
        CourierCorrelationConventions.Register();

        AssertTrackingCorrelation<IRoutingSlip>();
        AssertTrackingCorrelation<IRoutingSlipCompleted>();
        AssertTrackingCorrelation<IRoutingSlipFaulted>();
        AssertExecutionCorrelation<IRoutingSlipActivityCompleted>();
        AssertExecutionCorrelation<IRoutingSlipActivityFaulted>();
        AssertExecutionCorrelation<IRoutingSlipActivityCompensated>();
        AssertExecutionCorrelation<IRoutingSlipActivityCompensationFailed>();
        AssertTrackingCorrelation<IRoutingSlipCompensationFailed>();
        AssertTrackingCorrelation<IRoutingSlipTerminated>();
        AssertTrackingCorrelation<IRoutingSlipRevised>();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-JSON", "all-fifteen-contracts-have-the-exact-concrete-json-mapping")]
    public void JsonMappings_RegisterEveryCourierContractToItsExactConcreteRepresentation()
    {
        CourierJsonTypeMappings.Register();

        Assert.Equal(15, JsonMappings.Length);
        Assert.All(JsonMappings, mapping => AssertExactMapping(mapping.Contract, mapping.Implementation));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-JSON", "every-courier-contract-materializes-through-its-mapped-type-including-json-null")]
    public void JsonMappings_MaterializeEveryConcreteRepresentationAndPreserveNull()
    {
        CourierJsonTypeMappings.Register();
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SystemTextJsonConverterFactory());

        foreach ((Type contract, Type implementation) in JsonMappings)
        {
            object? materialized = JsonSerializer.Deserialize("{}", contract, options);
            object? nullValue = JsonSerializer.Deserialize("null", contract, options);

            Assert.NotNull(materialized);
            Assert.Equal(implementation, materialized.GetType());
            Assert.Null(nullValue);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-JSON", "conflicting-courier-mapping-is-rejected-without-replacing-the-canonical-type")]
    public void JsonMappingConflict_IsRejectedWithoutReplacingTheCanonicalCourierMapping()
    {
        CourierJsonTypeMappings.Register();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            JsonMessageTypeMappingRegistry.Register<IRoutingSlipCompleted, AlternateRoutingSlipCompleted>());

        Assert.Contains(typeof(IRoutingSlipCompleted).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(RoutingSlipCompletedMessage).FullName!, exception.Message, StringComparison.Ordinal);
        AssertExactMapping(typeof(IRoutingSlipCompleted), typeof(RoutingSlipCompletedMessage));
    }

    private static MethodInfo AssertRegistrarShape(Type registrar)
    {
        Assert.False(registrar.IsPublic);
        Assert.True(registrar.IsAbstract);
        Assert.True(registrar.IsSealed);
        Assert.Empty(registrar.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));

        MethodInfo register = Assert.Single(
            registrar.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Equal("Register", register.Name);
        Assert.True(register.IsAssembly);
        Assert.Equal(typeof(void), register.ReturnType);
        Assert.Empty(register.GetParameters());
        return register;
    }

    private static void AssertTrackingCorrelation<T>()
        where T : class
    {
        IMessageCorrelationId<T> resolver = GetCorrelationResolver<T>();
        T selected = CreateCorrelationContract<T>(TrackingNumber, ExecutionId);
        T empty = CreateCorrelationContract<T>(Guid.Empty, ExecutionId);

        Assert.True(resolver.TryGetCorrelationId(selected, out Guid actual));
        Assert.Equal(TrackingNumber, actual);
        Assert.False(resolver.TryGetCorrelationId(empty, out Guid emptyValue));
        Assert.Equal(Guid.Empty, emptyValue);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(
            () => resolver.TryGetCorrelationId(null!, out _)).ParamName);
    }

    private static void AssertExecutionCorrelation<T>()
        where T : class
    {
        IMessageCorrelationId<T> resolver = GetCorrelationResolver<T>();
        T selected = CreateCorrelationContract<T>(TrackingNumber, ExecutionId);
        T empty = CreateCorrelationContract<T>(TrackingNumber, Guid.Empty);

        Assert.True(resolver.TryGetCorrelationId(selected, out Guid actual));
        Assert.Equal(ExecutionId, actual);
        Assert.False(resolver.TryGetCorrelationId(empty, out Guid emptyValue));
        Assert.Equal(Guid.Empty, emptyValue);
        Assert.Equal("message", Assert.Throws<ArgumentNullException>(
            () => resolver.TryGetCorrelationId(null!, out _)).ParamName);
    }

    private static IMessageCorrelationId<T> GetCorrelationResolver<T>()
        where T : class
    {
        Assert.True(GlobalTopology.Send.GetMessageTopology<T>().TryGetConvention(
            out ICorrelationIdMessageSendTopologyConvention<T>? convention));
        Assert.NotNull(convention);
        Assert.True(convention.TryGetCorrelationIdResolver(out IMessageCorrelationId<T>? resolver));
        return Assert.IsAssignableFrom<IMessageCorrelationId<T>>(resolver);
    }

    private static T CreateCorrelationContract<T>(Guid trackingNumber, Guid executionId)
        where T : class
    {
        T contract = DispatchProxy.Create<T, CorrelationContractProxy>();
        var proxy = (CorrelationContractProxy)(object)contract;
        proxy.TrackingNumber = trackingNumber;
        proxy.ExecutionId = executionId;
        return contract;
    }

    private static void AssertExactMapping(Type contract, Type implementation)
    {
        Assert.True(JsonMessageTypeMappingRegistry.Contains(contract));
        Assert.True(JsonMessageTypeMappingRegistry.TryCreateConverter(contract, out JsonConverter? converter));
        Assert.NotNull(converter);
        Type converterType = converter.GetType();
        Assert.True(converterType.IsGenericType);
        Assert.Equal([contract, implementation], converterType.GetGenericArguments());
    }

    private class CorrelationContractProxy : DispatchProxy
    {
        public Guid TrackingNumber { get; set; }

        public Guid ExecutionId { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_TrackingNumber" => TrackingNumber,
            "get_ExecutionId" => ExecutionId,
            _ => throw new NotSupportedException($"Unexpected correlation-contract member: {targetMethod?.Name}"),
        };
    }

    private sealed class AlternateRoutingSlipCompleted : IRoutingSlipCompleted
    {
        public Guid TrackingNumber { get; init; }

        public DateTimeOffset Timestamp { get; init; }

        public TimeSpan Duration { get; init; }

        public IReadOnlyDictionary<string, object> Variables { get; init; } =
            new Dictionary<string, object>();
    }
}
