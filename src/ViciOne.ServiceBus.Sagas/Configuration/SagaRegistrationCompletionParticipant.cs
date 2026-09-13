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

    public int Order => 100;

    public ISagaRepositoryRegistrationProvider Provider { get; set; } = new MissingSagaRepositoryRegistrationProvider();

    public static SagaRegistrationCompletionParticipant Ensure(IRegistrationConfigurator configurator) =>
        configurator.GetOrAddRegistrationCompletionParticipant(static () => new SagaRegistrationCompletionParticipant());

    public static void RequireRepository<TSaga>(IRegistrationConfigurator configurator)
        where TSaga : class, ISaga =>
        Ensure(configurator)._repositoryOnlySagaTypes.Add(typeof(TSaga));

    public void Complete(IRegistrationConfigurator configurator)
    {
        IContainerRegistrar registrar = configurator.Advanced().Registrar;
        List<ISagaRegistration> registrations = registrar.GetRegistrations<ISagaRegistration>().ToList();

        foreach (ISagaRegistration registration in registrations)
        {
            if (HasRepository(configurator, registration.Type))
                continue;

            ConfigureRepository(configurator, registration.Type, Provider, registration);
        }

        foreach (Type sagaType in _repositoryOnlySagaTypes.OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            if (!HasRepository(configurator, sagaType))
                ConfigureRepository(configurator, sagaType, Provider, null);
        }
    }

    static bool HasRepository(IRegistrationConfigurator configurator, Type sagaType) =>
        configurator.Services.Any(x => x.ServiceType == typeof(ISagaRepositoryContextFactory<>).MakeGenericType(sagaType));

    static void ConfigureRepository(IRegistrationConfigurator configurator, Type sagaType,
        ISagaRepositoryRegistrationProvider provider, ISagaRegistration? registration)
    {
        var completion = (IConfigureSagaRepository)(Activator.CreateInstance(typeof(ConfigureSagaRepository<>).MakeGenericType(sagaType))
            ?? throw new InvalidOperationException("The requested runtime type could not be activated."));
        completion.Configure(configurator, provider, registration);
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
