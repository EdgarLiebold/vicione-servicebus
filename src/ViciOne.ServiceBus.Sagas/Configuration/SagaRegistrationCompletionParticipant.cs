using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection.Registration;

namespace ViciOne.ServiceBus.Configuration;

sealed class SagaRegistrationCompletionParticipant :
    IRegistrationCompletionParticipant
{
    readonly HashSet<Type> _repositoryOnlySagaTypes = new();
    ISagaRepositoryRegistrationProvider _provider = new MissingSagaRepositoryRegistrationProvider();

    public int Order => 100;

    public ISagaRepositoryRegistrationProvider Provider
    {
        get => _provider;
        set => _provider = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static SagaRegistrationCompletionParticipant Ensure(IRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        return configurator.GetOrAddRegistrationCompletionParticipant(static () => new SagaRegistrationCompletionParticipant());
    }

    public static void RequireRepository<TSaga>(IRegistrationConfigurator configurator)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);

        Ensure(configurator)._repositoryOnlySagaTypes.Add(typeof(TSaga));
    }

    public void Complete(IRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        ISagaRepositoryRegistrationProvider provider = Provider;
        IContainerRegistrar registrar = configurator.Advanced().Registrar;
        var registrationsBySagaType = new Dictionary<Type, ISagaRegistration?>();

        foreach (ISagaRegistration registration in registrar.GetRegistrations<ISagaRegistration>())
        {
            if (registration == null)
                throw new InvalidOperationException("The saga registrar returned a null registration.");

            Type sagaType = registration.Type;
            ValidateSagaType(sagaType);
            registrationsBySagaType.TryAdd(sagaType, registration);
        }

        foreach (Type sagaType in _repositoryOnlySagaTypes)
            registrationsBySagaType.TryAdd(sagaType, null);

        List<(IConfigureSagaRepository Completion, ISagaRegistration? Registration)> configurations = registrationsBySagaType
            .Where(x => !HasRepository(configurator, x.Key))
            .OrderBy(x => x.Key.FullName, StringComparer.Ordinal)
            .ThenBy(x => x.Key.AssemblyQualifiedName, StringComparer.Ordinal)
            .Select(x => (CreateRepositoryConfiguration(x.Key), x.Value))
            .ToList();

        foreach ((IConfigureSagaRepository completion, ISagaRegistration? registration) in configurations)
        {
            completion.Configure(configurator, provider, registration);
        }
    }

    static bool HasRepository(IRegistrationConfigurator configurator, Type sagaType) =>
        configurator.Services.Any(x => x.ServiceType == typeof(ISagaRepositoryContextFactory<>).MakeGenericType(sagaType));

    static IConfigureSagaRepository CreateRepositoryConfiguration(Type sagaType)
    {
        ValidateSagaType(sagaType);

        Type configurationType = typeof(ConfigureSagaRepository<>).MakeGenericType(sagaType);
        return (IConfigureSagaRepository)Activator.CreateInstance(configurationType)!;
    }

    static void ValidateSagaType(Type sagaType)
    {
        if (sagaType == null)
            throw new InvalidOperationException("The saga registration did not specify a saga type.");

        if (sagaType.IsValueType || sagaType.ContainsGenericParameters || !typeof(ISaga).IsAssignableFrom(sagaType))
        {
            throw new InvalidOperationException(
                $"The saga registration type '{sagaType}' must be a closed reference type implementing {nameof(ISaga)}.");
        }
    }

    interface IConfigureSagaRepository
    {
        void Configure(IRegistrationConfigurator configurator, ISagaRepositoryRegistrationProvider provider,
            ISagaRegistration? registration);
    }

    sealed class ConfigureSagaRepository<TSaga> :
        IConfigureSagaRepository
        where TSaga : class, ISaga
    {
        public void Configure(IRegistrationConfigurator configurator, ISagaRepositoryRegistrationProvider provider,
            ISagaRegistration? registration)
        {
            var registrationConfigurator = new SagaRegistrationConfigurator<TSaga>(configurator, registration);
            provider.Configure(registrationConfigurator);
        }
    }
}
