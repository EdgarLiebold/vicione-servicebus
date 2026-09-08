using System;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DynamoDb.Configuration;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>Adds Amazon DynamoDB saga persistence to dependency-injection registrations.</summary>
public static class DynamoDbSagaRepositoryRegistrationExtensions
{
    /// <summary>Configures an Amazon DynamoDB repository for one versioned saga type.</summary>
    /// <typeparam name="TSaga">The versioned saga state persisted in Amazon DynamoDB.</typeparam>
    /// <param name="configurator">The saga registration to update.</param>
    /// <param name="configure">The callback that supplies the persistence context and repository settings.</param>
    /// <returns>The same saga registration configurator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configurator"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="ConfigurationException">The callback produces an invalid repository configuration.</exception>
    public static ISagaRegistrationConfigurator<TSaga> UseDynamoDb<TSaga>(
        this ISagaRegistrationConfigurator<TSaga> configurator,
        Action<IDynamoDbSagaRepositoryConfigurator<TSaga>> configure)
        where TSaga : class, ISagaVersion
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var repositoryConfigurator = new DynamoDbSagaRepositoryConfigurator<TSaga>();
        configure(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure(
            "The Amazon DynamoDB saga repository configuration is invalid:");

        configurator.Repository(registration => repositoryConfigurator.Register(registration));

        return configurator;
    }

    /// <summary>Uses Amazon DynamoDB for compatible versioned sagas discovered and registered by runtime type.</summary>
    /// <param name="configurator">The service registration configurator to update.</param>
    /// <param name="configure">The callback applied to every compatible saga repository.</param>
    /// <exception cref="ArgumentNullException"><paramref name="configurator"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static void UseDynamoDbForRegisteredSagas(
        this IRegistrationConfigurator configurator,
        Action<IDynamoDbSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        configurator.SetSagaRepositoryProvider(new DynamoDbSagaRepositoryRegistrationProvider(configure));
    }
}
