using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>
/// Provides extension methods for azure table repository registration.
/// </summary>
public static class AzureTableRepositoryRegistrationExtensions
{
    /// <summary>
    /// Adds a Azure Table saga repository to the registration
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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

    /// <summary>
    /// Configure the Job Service saga state machines to use Azure Table Storage
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IJobSagaRegistrationConfigurator AzureTableRepository(this IJobSagaRegistrationConfigurator configurator,
        Action<IAzureTableSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var registrationProvider = new AzureTableSagaRepositoryRegistrationProvider(configure);

        configurator.UseRepositoryRegistrationProvider(registrationProvider);

        return configurator;
    }

    /// <summary>
    /// Use the Azure Table saga repository for sagas configured by type (without a specific generic call to AddSaga/AddSagaStateMachine)
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure"></param>
    public static void SetAzureTableSagaRepositoryProvider(this IRegistrationConfigurator configurator,
        Action<IAzureTableSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        configurator.SetSagaRepositoryProvider(new AzureTableSagaRepositoryRegistrationProvider(configure));
    }
}
