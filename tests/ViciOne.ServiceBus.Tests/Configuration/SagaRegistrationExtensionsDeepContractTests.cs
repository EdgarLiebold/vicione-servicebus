using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration
{
    public sealed class SagaRegistrationExtensionsDeepContractTests
    {
        private static readonly Func<Type, bool> AcceptAll = static _ => true;

        [Fact]
        [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-EXTENSIONS", "bulk-registration-exact-public-surface")]
        public void PublicSurface_ContainsTheExactNaturalAndExplicitRegistrationFamilies()
        {
            Type extensions = typeof(SagaRegistrationExtensions);
            MethodInfo[] methods = extensions.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            Assert.True(extensions.IsPublic);
            Assert.True(extensions.IsAbstract);
            Assert.True(extensions.IsSealed);
            Assert.Equal(16, methods.Length);
            Assert.Equal(1, methods.Count(method => method.Name == nameof(SagaRegistrationExtensions.AddSaga)));
            Assert.Equal(6, methods.Count(method => method.Name == nameof(SagaRegistrationExtensions.AddSagas)));
            Assert.Equal(2, methods.Count(method => method.Name == nameof(SagaRegistrationExtensions.AddSagasFromNamespaceContaining)));
            Assert.Equal(1, methods.Count(method => method.Name == nameof(SagaRegistrationExtensions.AddSagaStateMachine)));
            Assert.Equal(4, methods.Count(method => method.Name == nameof(SagaRegistrationExtensions.AddSagaStateMachines)));
            Assert.Equal(2, methods.Count(method => method.Name == nameof(SagaRegistrationExtensions.AddSagaStateMachinesFromNamespaceContaining)));
            Assert.All(methods, method => Assert.True(method.IsDefined(typeof(ExtensionAttribute), inherit: false)));

            AssertMethod(methods, nameof(SagaRegistrationExtensions.AddSagas), typeof(IRegistrationConfigurator));
            AssertMethod(
                methods,
                nameof(SagaRegistrationExtensions.AddSagas),
                typeof(IRegistrationConfigurator),
                typeof(Func<Type, bool>));
            AssertMethod(methods, nameof(SagaRegistrationExtensions.AddSagaStateMachines), typeof(IRegistrationConfigurator));

            (ServiceCollection explicitServices, IRegistrationConfigurator explicitConfigurator) = CreateConfiguration();
            int explicitBaseline = explicitServices.Count;
            SagaRegistrationExtensions.AddSagas(explicitConfigurator, AcceptAll, typeof(string).Assembly);
            SagaRegistrationExtensions.AddSagas(explicitConfigurator, typeof(string).Assembly);
            SagaRegistrationExtensions.AddSagaStateMachines(explicitConfigurator, typeof(string).Assembly);
            Assert.Equal(explicitBaseline, explicitServices.Count);

            (ServiceCollection filteredServices, IRegistrationConfigurator filteredConfigurator) = CreateConfiguration();
            SagaRegistrationExtensions.AddSagas(filteredConfigurator, static type => type == typeof(SecondSaga));
            Assert.Equal(typeof(SecondSaga), Assert.Single(GetRegistrations(filteredServices)).Type);

            (ServiceCollection naturalServices, IRegistrationConfigurator naturalConfigurator) = CreateConfiguration();
            int naturalBaseline = naturalServices.Count;
            AssertArgument("types", () => SagaRegistrationExtensions.AddSagas(naturalConfigurator));
            Assert.Equal(naturalBaseline, naturalServices.Count);
            AssertArgument("types", () => SagaRegistrationExtensions.AddSagaStateMachines(naturalConfigurator));
            Assert.Equal(naturalBaseline, naturalServices.Count);
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-EXTENSIONS", "bulk-registration-guard-order-and-null-elements")]
        public void RequiredBoundaries_ValidateReceiverFilterArraysAndElementsBeforeEffects()
        {
            Action[] missingReceiverCalls =
            [
                () => SagaRegistrationExtensions.AddSaga<FirstSaga, FirstSagaDefinition>(null!),
                () => SagaRegistrationExtensions.AddSagas(null!),
                () => SagaRegistrationExtensions.AddSagas(null!, AcceptAll),
                () => SagaRegistrationExtensions.AddSagas(null!, AcceptAll, Array.Empty<Assembly>()),
                () => SagaRegistrationExtensions.AddSagas(null!, Array.Empty<Assembly>()),
                () => SagaRegistrationExtensions.AddSagasFromNamespaceContaining<OrdinalNamespace.Anchor>(null!),
                () => SagaRegistrationExtensions.AddSagasFromNamespaceContaining(null!, null!),
                () => SagaRegistrationExtensions.AddSagas(null!, Array.Empty<Type>()),
                () => SagaRegistrationExtensions.AddSagas(null!, null, Array.Empty<Type>()),
                () => SagaRegistrationExtensions.AddSagaStateMachine<FirstStateMachine, FirstState, FirstStateDefinition>(null!),
                () => SagaRegistrationExtensions.AddSagaStateMachines(null!),
                () => SagaRegistrationExtensions.AddSagaStateMachines(null!, Array.Empty<Assembly>()),
                () => SagaRegistrationExtensions.AddSagaStateMachinesFromNamespaceContaining<OrdinalNamespace.Anchor>(null!),
                () => SagaRegistrationExtensions.AddSagaStateMachinesFromNamespaceContaining(null!, null!),
                () => SagaRegistrationExtensions.AddSagaStateMachines(null!, Array.Empty<Type>()),
                () => SagaRegistrationExtensions.AddSagaStateMachines(null!, null, Array.Empty<Type>()),
            ];

            Assert.All(missingReceiverCalls, call => Assert.Equal(
                "configurator",
                Assert.Throws<ArgumentNullException>(call).ParamName));

            (ServiceCollection services, IRegistrationConfigurator configurator) = CreateConfiguration();
            int baseline = services.Count;

            AssertArgumentNull("filter", () => SagaRegistrationExtensions.AddSagas(configurator, (Func<Type, bool>)null!));
            AssertArgumentNull("filter", () => SagaRegistrationExtensions.AddSagas(
                configurator,
                null!,
                (Assembly[])null!));
            AssertArgumentNull("assemblies", () => SagaRegistrationExtensions.AddSagas(configurator, (Assembly[])null!));
            AssertArgument("assemblies", () => SagaRegistrationExtensions.AddSagas(
                configurator,
                new[] { typeof(FirstSaga).Assembly, null! }));
            AssertArgumentNull("types", () => SagaRegistrationExtensions.AddSagas(configurator, (Type[])null!));
            AssertArgument("types", () => SagaRegistrationExtensions.AddSagas(
                configurator,
                new[] { typeof(FirstSaga), null! }));
            AssertArgumentNull("assemblies", () => SagaRegistrationExtensions.AddSagaStateMachines(
                configurator,
                (Assembly[])null!));
            AssertArgument("assemblies", () => SagaRegistrationExtensions.AddSagaStateMachines(
                configurator,
                new[] { typeof(FirstSaga).Assembly, null! }));
            AssertArgumentNull("types", () => SagaRegistrationExtensions.AddSagaStateMachines(configurator, (Type[])null!));
            AssertArgument("types", () => SagaRegistrationExtensions.AddSagaStateMachines(
                configurator,
                new[] { typeof(FirstStateMachine), null! }));

            Assert.Equal(baseline, services.Count);
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-EXTENSIONS", "explicit-selection-closed-concrete-and-owned-exclusion")]
        public void ExplicitSelection_RegistersOnlyClosedConcreteCandidatesAndExcludesOwnedStateMachines()
        {
            (ServiceCollection sagaServices, IRegistrationConfigurator sagaConfigurator) = CreateConfiguration();

            SagaRegistrationExtensions.AddSagas(
                sagaConfigurator,
                null,
                new[]
                {
                    typeof(FirstSaga),
                    typeof(FirstSagaDefinition),
                    typeof(AbstractSaga),
                    typeof(OpenSaga<>),
                    typeof(ISaga),
                    typeof(FirstState),
                });

            ISagaRegistration sagaRegistration = Assert.Single(GetRegistrations(sagaServices));
            Assert.Equal(typeof(FirstSaga), sagaRegistration.Type);
            Assert.Null(sagaRegistration.StateMachineType);
            Assert.Contains(sagaServices, descriptor => descriptor.ServiceType == typeof(FirstSagaDefinition));

            (ServiceCollection stateServices, IRegistrationConfigurator stateConfigurator) = CreateConfiguration();
            SagaRegistrationExtensions.AddSagaStateMachines(
                stateConfigurator,
                null,
                new[]
                {
                    typeof(FirstStateMachine),
                    typeof(FirstStateDefinition),
                    typeof(AbstractStateMachine),
                    typeof(OpenStateMachine<>),
                    typeof(OwnedStateMachine),
                });

            ISagaRegistration stateRegistration = Assert.Single(GetRegistrations(stateServices));
            Assert.Equal(typeof(FirstState), stateRegistration.Type);
            Assert.Equal(typeof(FirstStateMachine), stateRegistration.StateMachineType);
            Assert.Contains(stateServices, descriptor => descriptor.ServiceType == typeof(FirstStateDefinition));
            Assert.DoesNotContain(stateServices, descriptor => descriptor.ServiceType == typeof(OwnedStateMachine));
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-EXTENSIONS", "duplicate-definition-rejection-before-effects")]
        public void DuplicateDefinitions_AreRejectedBeforeEitherRegistrationFamilyMutatesServices()
        {
            (ServiceCollection sagaServices, IRegistrationConfigurator sagaConfigurator) = CreateConfiguration();
            int sagaBaseline = sagaServices.Count;

            ArgumentException sagaException = Assert.Throws<ArgumentException>(() => SagaRegistrationExtensions.AddSagas(
                sagaConfigurator,
                typeof(FirstSaga),
                typeof(FirstSagaDefinition),
                typeof(AlternateFirstSagaDefinition)));

            Assert.Equal("types", sagaException.ParamName);
            Assert.Contains(
                $"Multiple saga definitions target {TypeCache.GetShortName(typeof(FirstSaga))}",
                sagaException.Message,
                StringComparison.Ordinal);
            Assert.Equal(sagaBaseline, sagaServices.Count);

            (ServiceCollection stateServices, IRegistrationConfigurator stateConfigurator) = CreateConfiguration();
            int stateBaseline = stateServices.Count;

            ArgumentException stateException = Assert.Throws<ArgumentException>(() => SagaRegistrationExtensions.AddSagaStateMachines(
                stateConfigurator,
                typeof(FirstStateMachine),
                typeof(FirstStateDefinition),
                typeof(AlternateFirstStateDefinition)));

            Assert.Equal("types", stateException.ParamName);
            Assert.Contains(
                $"Multiple saga definitions target {TypeCache.GetShortName(typeof(FirstState))}",
                stateException.Message,
                StringComparison.Ordinal);
            Assert.Equal(stateBaseline, stateServices.Count);
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-EXTENSIONS", "registration-planning-failure-atomicity")]
        public void PlanningFailure_AfterAnEarlierCandidateLeavesBothRegistrationFamiliesUntouched()
        {
            (ServiceCollection sagaServices, IRegistrationConfigurator sagaConfigurator) = CreateConfiguration();
            int sagaBaseline = sagaServices.Count;

            Assert.Throws<PlanningException>(() => SagaRegistrationExtensions.AddSagas(
                sagaConfigurator,
                type => type == typeof(SecondSaga) ? throw new PlanningException() : true,
                typeof(FirstSaga),
                typeof(SecondSaga)));
            Assert.Equal(sagaBaseline, sagaServices.Count);

            (ServiceCollection stateServices, IRegistrationConfigurator stateConfigurator) = CreateConfiguration();
            int stateBaseline = stateServices.Count;

            Assert.Throws<PlanningException>(() => SagaRegistrationExtensions.AddSagaStateMachines(
                stateConfigurator,
                type => type == typeof(SecondStateMachine) ? throw new PlanningException() : true,
                typeof(FirstStateMachine),
                typeof(SecondStateMachine)));
            Assert.Equal(stateBaseline, stateServices.Count);
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-EXTENSIONS", "namespace-selection-ordinal-case-sensitive")]
        public void NamespaceSelection_IncludesExactAndChildNamespacesButExcludesDifferentCasing()
        {
            (ServiceCollection services, IRegistrationConfigurator configurator) = CreateConfiguration();

            SagaRegistrationExtensions.AddSagasFromNamespaceContaining(
                configurator,
                typeof(OrdinalNamespace.Anchor));

            Type[] registeredTypes = GetRegistrations(services).Select(registration => registration.Type).ToArray();
            Assert.Contains(typeof(OrdinalNamespace.ExactSaga), registeredTypes);
            Assert.Contains(typeof(OrdinalNamespace.Child.ChildSaga), registeredTypes);
            Assert.DoesNotContain(typeof(ordinalnamespace.WrongCaseSaga), registeredTypes);
        }

        [Fact]
        [RequirementCoverage("REQ-VSB-SAGA-REGISTRATION-EXTENSIONS", "nullable-type-filter-and-state-machine-or-semantics")]
        public void TypeFilters_AcceptNullAndSelectStateMachinesByEitherMachineOrStateType()
        {
            (ServiceCollection unfilteredServices, IRegistrationConfigurator unfilteredConfigurator) = CreateConfiguration();
            SagaRegistrationExtensions.AddSagas(
                unfilteredConfigurator,
                null,
                new[] { typeof(FirstSaga), typeof(SecondSaga) });
            Assert.Equal(
                [typeof(FirstSaga), typeof(SecondSaga)],
                GetRegistrations(unfilteredServices).Select(registration => registration.Type));

            (ServiceCollection filteredServices, IRegistrationConfigurator filteredConfigurator) = CreateConfiguration();
            SagaRegistrationExtensions.AddSagas(
                filteredConfigurator,
                type => type == typeof(SecondSaga),
                typeof(FirstSaga),
                typeof(SecondSaga));
            Assert.Equal(typeof(SecondSaga), Assert.Single(GetRegistrations(filteredServices)).Type);

            (ServiceCollection stateServices, IRegistrationConfigurator stateConfigurator) = CreateConfiguration();
            SagaRegistrationExtensions.AddSagaStateMachines(
                stateConfigurator,
                type => type == typeof(FirstState),
                typeof(FirstStateMachine),
                typeof(FirstStateDefinition));

            ISagaRegistration stateRegistration = Assert.Single(GetRegistrations(stateServices));
            Assert.Equal(typeof(FirstState), stateRegistration.Type);
            Assert.Equal(typeof(FirstStateMachine), stateRegistration.StateMachineType);
            Assert.Contains(stateServices, descriptor => descriptor.ServiceType == typeof(FirstStateDefinition));
        }

        private static void AssertMethod(IEnumerable<MethodInfo> methods, string name, params Type[] parameterTypes)
        {
            MethodInfo method = Assert.Single(methods, candidate =>
                candidate.Name == name
                && !candidate.IsGenericMethod
                && candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameterTypes));

            Assert.Equal(typeof(void), method.ReturnType);
        }

        private static void AssertArgumentNull(string parameterName, Action action) =>
            Assert.Equal(parameterName, Assert.Throws<ArgumentNullException>(action).ParamName);

        private static void AssertArgument(string parameterName, Action action) =>
            Assert.Equal(parameterName, Assert.Throws<ArgumentException>(action).ParamName);

        private static (ServiceCollection Services, IRegistrationConfigurator Configurator) CreateConfiguration()
        {
            var services = new ServiceCollection();
            return (services, new ServiceCollectionBusConfigurator(services));
        }

        private static ISagaRegistration[] GetRegistrations(IServiceCollection services) =>
            services
                .Where(descriptor => descriptor.ServiceType == typeof(ISagaRegistration))
                .Select(descriptor => Assert.IsAssignableFrom<ISagaRegistration>(descriptor.ImplementationInstance))
                .ToArray();

        private sealed class PlanningException : Exception
        {
        }

        public sealed class FirstSaga : ISaga
        {
            public Guid CorrelationId { get; set; }
        }

        public sealed class SecondSaga : ISaga
        {
            public Guid CorrelationId { get; set; }
        }

        public abstract class AbstractSaga : ISaga
        {
            public Guid CorrelationId { get; set; }
        }

        public sealed class OpenSaga<T> : ISaga
        {
            public Guid CorrelationId { get; set; }
        }

        public sealed class FirstSagaDefinition : SagaDefinition<FirstSaga>
        {
        }

        public sealed class AlternateFirstSagaDefinition : SagaDefinition<FirstSaga>
        {
        }

        public sealed class FirstState : ISagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }
        }

        public sealed class SecondState : ISagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }
        }

        public sealed class OwnedState : ISagaStateMachineInstance, IConsumerKindOwnedState
        {
            public Guid CorrelationId { get; set; }
        }

        public sealed class FirstStateMachine : ViciOneServiceBusStateMachine<FirstState>
        {
        }

        public sealed class SecondStateMachine : ViciOneServiceBusStateMachine<SecondState>
        {
        }

        public abstract class AbstractStateMachine : ViciOneServiceBusStateMachine<FirstState>
        {
        }

        public sealed class OpenStateMachine<TState> : ViciOneServiceBusStateMachine<TState>
            where TState : class, ISagaStateMachineInstance
        {
        }

        public sealed class OwnedStateMachine : ViciOneServiceBusStateMachine<OwnedState>
        {
        }

        public sealed class FirstStateDefinition : SagaDefinition<FirstState>
        {
        }

        public sealed class AlternateFirstStateDefinition : SagaDefinition<FirstState>
        {
        }
    }
}

namespace ViciOne.ServiceBus.Tests.Configuration.OrdinalNamespace
{
    public sealed class Anchor
    {
    }

    public sealed class ExactSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}

namespace ViciOne.ServiceBus.Tests.Configuration.OrdinalNamespace.Child
{
    public sealed class ChildSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}

namespace ViciOne.ServiceBus.Tests.Configuration.ordinalnamespace
{
    public sealed class WrongCaseSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
