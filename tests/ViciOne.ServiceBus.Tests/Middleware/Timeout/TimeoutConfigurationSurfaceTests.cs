using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Timeout;

public sealed class TimeoutConfigurationSurfaceTests
{
    private static readonly TimeProvider ConfiguredTimeProvider = new FakeTimeProvider(
        new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.Zero));

    [Fact]
    [RequirementCoverage("REQ-VSB-TIMEOUT-CONFIGURATION", "public-capabilities-and-internal-structure")]
    public void PublicSurface_PreservesEverySupportedScopeAndHidesItsImplementationTypes()
    {
        MethodInfo[] overloads = typeof(TimeoutConfiguratorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(TimeoutConfiguratorExtensions.UseTimeout))
            .ToArray();
        string[] configuredScopes = overloads
            .Select(method => GenericDefinitionName(method.GetParameters()[0].ParameterType))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.All(overloads, overload =>
        {
            ParameterInfo[] parameters = overload.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.False(parameters[1].HasDefaultValue);
            Assert.Equal(typeof(Action<ITimeoutConfigurator>), parameters[1].ParameterType);
        });

        Assert.Equal(
            [
                typeof(IConsumePipeConfigurator).FullName!,
                typeof(IConsumerConfigurator<>).FullName!,
                typeof(IHandlerConfigurator<>).FullName!,
                typeof(IPipeConfigurator<>).FullName!,
                typeof(ISagaConfigurator<>).FullName!,
            ],
            configuredScopes);

        PropertyInfo[] options = typeof(ITimeoutConfigurator).GetProperties();
        Assert.Equal([nameof(ITimeoutConfigurator.TimeProvider), nameof(ITimeoutConfigurator.Timeout)],
            options.Select(property => property.Name).Order(StringComparer.Ordinal));
        Assert.All(options, property =>
        {
            Assert.Null(property.GetMethod);
            Assert.NotNull(property.SetMethod);
        });

        Type productAssemblyAnchor = typeof(TimeoutFilter<,>);
        Assert.True(productAssemblyAnchor.IsPublic);
        Assert.True(productAssemblyAnchor.IsSealed);
        Assert.Equal([2, 3], productAssemblyAnchor.GetConstructors().Select(constructor => constructor.GetParameters().Length).Order());

        Type commonSpecification = RequiredType("ViciOne.ServiceBus.Configuration.TimeoutPipeSpecification`2");
        Assert.True(commonSpecification.IsAbstract);
        Assert.True(commonSpecification.IsNotPublic);

        string[] specificationNames =
        [
            "ViciOne.ServiceBus.Configuration.TimeoutSpecification`1",
            "ViciOne.ServiceBus.Configuration.ExecuteContextTimeoutSpecification`1",
            "ViciOne.ServiceBus.Configuration.CompensateContextTimeoutSpecification`1",
        ];
        foreach (string specificationName in specificationNames)
        {
            Type specification = RequiredType(specificationName);
            Assert.True(specification.IsNotPublic);
            Assert.True(specification.IsSealed);
            Assert.Equal(commonSpecification, specification.BaseType!.GetGenericTypeDefinition());
        }

        string[] observerNames =
        [
            "ViciOne.ServiceBus.Configuration.TimeoutConfigurationObserver",
            "ViciOne.ServiceBus.Configuration.TimeoutConsumerConfigurationObserver`1",
            "ViciOne.ServiceBus.Configuration.TimeoutHandlerConfigurationObserver",
            "ViciOne.ServiceBus.Configuration.TimeoutSagaConfigurationObserver`1",
        ];
        Assert.All(observerNames.Select(RequiredType), observer =>
        {
            Assert.True(observer.IsNotPublic);
            Assert.True(observer.IsSealed);
        });

        Assert.All(
            new[]
            {
                RequiredType("ViciOne.ServiceBus.Middleware.Timeout.TimeoutConsumeContext`1"),
                RequiredType("ViciOne.ServiceBus.Middleware.Timeout.TimeoutCourierContextProxy"),
                RequiredType("ViciOne.ServiceBus.Middleware.Timeout.TimeoutExecuteContext`1"),
                RequiredType("ViciOne.ServiceBus.Middleware.Timeout.TimeoutCompensateContext`1"),
            },
            contextType => Assert.True(contextType.IsNotPublic));
    }

    public static TheoryData<string, string> SupportedScopeSpecifications => new()
    {
        { "message", "ViciOne.ServiceBus.Configuration.TimeoutSpecification`1" },
        { "consume", "ViciOne.ServiceBus.Configuration.TimeoutSpecification`1" },
        { "consumer", "ViciOne.ServiceBus.Configuration.TimeoutSpecification`1" },
        { "handler", "ViciOne.ServiceBus.Configuration.TimeoutSpecification`1" },
        { "saga", "ViciOne.ServiceBus.Configuration.TimeoutSpecification`1" },
        { "execute", "ViciOne.ServiceBus.Configuration.ExecuteContextTimeoutSpecification`1" },
        { "compensate", "ViciOne.ServiceBus.Configuration.CompensateContextTimeoutSpecification`1" },
    };

    [Theory]
    [MemberData(nameof(SupportedScopeSpecifications))]
    [RequirementCoverage("REQ-VSB-TIMEOUT-CONFIGURATION", "scope-wiring")]
    public void EverySupportedScope_CreatesOneValidSpecification(string scope, string expectedSpecification)
    {
        object specification = ConfigureScope(scope, TimeSpan.FromMinutes(1));
        var validation = Assert.IsAssignableFrom<IEnumerable<ValidationResult>>(
            specification.GetType().GetMethod("Validate")!.Invoke(specification, null));

        Assert.Equal(expectedSpecification, specification.GetType().GetGenericTypeDefinition().FullName);
        Assert.Empty(validation);
    }

    [Theory]
    [MemberData(nameof(SupportedScopeSpecifications))]
    [RequirementCoverage("REQ-VSB-TIMEOUT-CONFIGURATION", "nonpositive-timeout-rejection")]
    public void EverySupportedScope_RejectsANonpositiveTimeout(string scope, string _)
    {
        object specification = ConfigureScope(scope, TimeSpan.Zero);
        var validation = Assert.IsAssignableFrom<IEnumerable<ValidationResult>>(
            specification.GetType().GetMethod("Validate")!.Invoke(specification, null));

        ValidationResult failure = Assert.Single(validation);
        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Equal(nameof(ITimeoutConfigurator.Timeout), failure.Key);
        Assert.Contains("greater than zero", failure.Message, StringComparison.Ordinal);
    }

    private static string GenericDefinitionName(Type type) => type.IsGenericType
        ? type.GetGenericTypeDefinition().FullName!
        : type.FullName!;

    private static Type RequiredType(string fullName) =>
        typeof(TimeoutFilter<,>).Assembly.GetType(fullName, throwOnError: true)!;

    private static object ConfigureScope(string scope, TimeSpan configuredTimeout)
    {
        var state = new ConfigurationState();
        void Configure(ITimeoutConfigurator timeout)
        {
            timeout.Timeout = configuredTimeout;
            timeout.TimeProvider = ConfiguredTimeProvider;
        }

        switch (scope)
        {
            case "message":
                CreateProxy<IPipeConfigurator<ConsumeContext<ConfigurationMessage>>>(state).UseTimeout(Configure);
                break;

            case "consume":
                {
                    IConsumePipeConfigurator configurator = CreateProxy<IConsumePipeConfigurator>(state);
                    configurator.UseTimeout(Configure);
                    state.Observer<IHandlerConfigurationObserver>().HandlerConfigured(
                        CreateProxy<IHandlerConfigurator<ConfigurationMessage>>(state));
                    break;
                }

            case "consumer":
                {
                    IConsumerConfigurator<ConfigurationConsumer> configurator =
                        CreateProxy<IConsumerConfigurator<ConfigurationConsumer>>(state);
                    configurator.UseTimeout(Configure);
                    state.Observer<IConsumerConfigurationObserver>().ConsumerMessageConfigured<ConfigurationConsumer, ConfigurationMessage>(
                        CreateProxy<IConsumerMessageConfigurator<ConfigurationConsumer, ConfigurationMessage>>(state));
                    break;
                }

            case "handler":
                {
                    IHandlerConfigurator<ConfigurationMessage> configurator =
                        CreateProxy<IHandlerConfigurator<ConfigurationMessage>>(state);
                    configurator.UseTimeout(Configure);
                    state.Observer<IHandlerConfigurationObserver>().HandlerConfigured(configurator);
                    break;
                }

            case "saga":
                {
                    ISagaConfigurator<ConfigurationSaga> configurator = CreateProxy<ISagaConfigurator<ConfigurationSaga>>(state);
                    configurator.UseTimeout(Configure);
                    state.Observer<ISagaConfigurationObserver>().SagaMessageConfigured<ConfigurationSaga, ConfigurationMessage>(
                        CreateProxy<ISagaMessageConfigurator<ConfigurationSaga, ConfigurationMessage>>(state));
                    break;
                }

            case "execute":
                {
                    IConsumePipeConfigurator configurator = CreateProxy<IConsumePipeConfigurator>(state);
                    configurator.UseTimeout(Configure);
                    state.Observer<IActivityConfigurationObserver>().ExecuteActivityConfigured<ConfigurationActivity, ConfigurationArguments>(
                        CreateProxy<IExecuteActivityConfigurator<ConfigurationActivity, ConfigurationArguments>>(state));
                    break;
                }

            case "compensate":
                {
                    IConsumePipeConfigurator configurator = CreateProxy<IConsumePipeConfigurator>(state);
                    configurator.UseTimeout(Configure);
                    state.Observer<IActivityConfigurationObserver>().CompensateActivityConfigured<ConfigurationActivity, ConfigurationLog>(
                        CreateProxy<ICompensateActivityConfigurator<ConfigurationActivity, ConfigurationLog>>(state));
                    break;
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown timeout configuration scope.");
        }

        return Assert.Single(state.Specifications);
    }

    private static T CreateProxy<T>(ConfigurationState state)
        where T : class
    {
        T proxy = DispatchProxy.Create<T, ConfigurationProxy>();
        ((ConfigurationProxy)(object)proxy).State = state;
        return proxy;
    }

    private static object CreateProxy(Type interfaceType, ConfigurationState state)
    {
        object proxy = DispatchProxy.Create(interfaceType, typeof(ConfigurationProxy));
        ((ConfigurationProxy)proxy).State = state;
        return proxy;
    }

    private sealed class ConfigurationState
    {
        public List<object> Specifications { get; } = [];
        public List<object> Observers { get; } = [];

        public T Observer<T>() where T : class => Assert.Single(Observers.OfType<T>().Distinct());
    }

    private class ConfigurationProxy : DispatchProxy
    {
        public ConfigurationState? State { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ConfigurationState state = State ?? throw new InvalidOperationException("The configuration proxy was not initialized.");

            if (targetMethod.Name.StartsWith("Connect", StringComparison.Ordinal)
                && targetMethod.Name.EndsWith("ConfigurationObserver", StringComparison.Ordinal))
            {
                state.Observers.Add(args![0]!);
                return new EmptyConnectHandle();
            }

            if (targetMethod.Name.StartsWith("AddPipeSpecification", StringComparison.Ordinal))
            {
                state.Specifications.Add(args![0]!);
                return null;
            }

            if (targetMethod.Name is "Message" or "Arguments" or "Log")
            {
                var configure = Assert.IsAssignableFrom<Delegate>(args![0]);
                Type configuratorType = configure.GetType().GetGenericArguments()[0];
                configure.DynamicInvoke(CreateProxy(configuratorType, state));
                return null;
            }

            throw new NotSupportedException($"Unexpected configuration member: {targetMethod.Name}");
        }
    }

    private sealed class EmptyConnectHandle : ConnectHandle
    {
        public void Dispose()
        {
        }

        public void Disconnect()
        {
        }
    }

    private sealed record ConfigurationMessage(string Value);

    private sealed class ConfigurationConsumer : IConsumer<ConfigurationMessage>
    {
        public Task Consume(ConsumeContext<ConfigurationMessage> context) => Task.CompletedTask;
    }

    private sealed class ConfigurationSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed record ConfigurationArguments(string Value);

    private sealed record ConfigurationLog(string Value);

    private sealed class ConfigurationActivity : IActivity<ConfigurationArguments, ConfigurationLog>
    {
        public Task<ExecutionResult> Execute(ExecuteContext<ConfigurationArguments> context) =>
            throw new NotSupportedException();

        public Task<CompensationResult> Compensate(CompensateContext<ConfigurationLog> context) =>
            throw new NotSupportedException();
    }
}
