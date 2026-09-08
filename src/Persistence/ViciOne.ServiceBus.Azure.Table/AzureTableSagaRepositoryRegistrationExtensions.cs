using System;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table.Configuration;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Adds Azure Table saga persistence to dependency-injection registrations.</summary>
public static class AzureTableSagaRepositoryRegistrationExtensions
{
    /// <summary>Configures an Azure Table repository for one registered saga type.</summary>
    /// <typeparam name="TSaga">The saga state persisted in Azure Table Storage.</typeparam>
    /// <param name="configurator">The saga registration to update.</param>
    /// <param name="configure">The callback that supplies the table client and optional key formatter.</param>
    /// <returns>The same saga registration configurator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configurator"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="ConfigurationException">The callback produces an invalid repository configuration.</exception>
    public static ISagaRegistrationConfigurator<TSaga> UseAzureTable<TSaga>(
        this ISagaRegistrationConfigurator<TSaga> configurator,
        Action<IAzureTableSagaRepositoryConfigurator> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var repositoryConfigurator = new AzureTableSagaRepositoryConfigurator<TSaga>();

        configure(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The Azure Table saga repository configuration is invalid:");

        configurator.Repository(x => repositoryConfigurator.Register(x));

        return configurator;
    }

    /// <summary>Configures the Job Service saga state machines to use Azure Table Storage.</summary>
    /// <param name="configurator">The Job Service saga registration to update.</param>
    /// <param name="configure">The callback that supplies the table client for each Job Service saga type.</param>
    /// <returns>The same Job Service saga registration configurator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configurator"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IJobSagaRegistrationConfigurator UseAzureTable(
        this IJobSagaRegistrationConfigurator configurator,
        Action<IAzureTableSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var registrationProvider = new AzureTableSagaRepositoryRegistrationProvider(configure);

        configurator.UseRepositoryRegistrationProvider(registrationProvider);

        return configurator;
    }

    /// <summary>Uses Azure Table Storage for sagas discovered and registered by runtime type.</summary>
    /// <param name="configurator">The service registration configurator to update.</param>
    /// <param name="configure">The callback that supplies the Azure Table client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static void UseAzureTableForRegisteredSagas(
        this IRegistrationConfigurator configurator,
        Action<IAzureTableSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        configurator.SetSagaRepositoryProvider(new AzureTableSagaRepositoryRegistrationProvider(configure));
    }
}
