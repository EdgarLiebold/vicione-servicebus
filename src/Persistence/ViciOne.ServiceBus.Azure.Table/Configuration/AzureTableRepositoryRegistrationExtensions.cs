using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>Adds Azure Table saga persistence to dependency-injection registrations.</summary>
public static class AzureTableRepositoryRegistrationExtensions
{
    /// <summary>Configures an Azure Table repository for one registered saga type.</summary>
    /// <typeparam name="T">The saga state persisted in Azure Table Storage.</typeparam>
    /// <param name="configurator">The saga registration to update.</param>
    /// <param name="configure">An optional callback that supplies the table client and key formatter.</param>
    /// <returns>The same saga registration configurator.</returns>
    public static ISagaRegistrationConfigurator<T> AzureTableRepository<T>(this ISagaRegistrationConfigurator<T> configurator,
        Action<IAzureTableSagaRepositoryConfigurator<T>>? configure = null)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var repositoryConfigurator = new AzureTableSagaRepositoryConfigurator<T>();

        configure?.Invoke(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The Azure Table saga repository configuration is invalid:");

        configurator.Repository(x => repositoryConfigurator.Register(x));

        return configurator;
    }

    /// <summary>Configure the Job Service saga state machines to use Azure Table Storage.</summary>
    /// <param name="configurator">The Job Service saga registration to update.</param>
    /// <param name="configure">The callback that supplies the table client for each Job Service saga type.</param>
    /// <returns>The same Job Service saga registration configurator.</returns>
    public static IJobSagaRegistrationConfigurator AzureTableRepository(this IJobSagaRegistrationConfigurator configurator,
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
    public static void SetAzureTableSagaRepositoryProvider(this IRegistrationConfigurator configurator,
        Action<IAzureTableSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        configurator.SetSagaRepositoryProvider(new AzureTableSagaRepositoryRegistrationProvider(configure));
    }
}
