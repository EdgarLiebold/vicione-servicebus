using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>Adds Amazon DynamoDB saga persistence to dependency-injection registrations.</summary>
public static class DynamoDbSagaRepositoryRegistrationExtensions
{
    /// <summary>Configures an Amazon DynamoDB repository for one versioned saga type.</summary>
    /// <typeparam name="T">The versioned saga state persisted in Amazon DynamoDB.</typeparam>
    /// <param name="configurator">The saga registration to update.</param>
    /// <param name="configure">An optional callback that supplies the persistence context and repository settings.</param>
    /// <returns>The same saga registration configurator.</returns>
    public static ISagaRegistrationConfigurator<T> DynamoDbRepository<T>(this ISagaRegistrationConfigurator<T> configurator,
        Action<IDynamoDbSagaRepositoryConfigurator<T>>? configure = null)
        where T : class, ISagaVersion
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var repositoryConfigurator = new DynamoDbSagaRepositoryConfigurator<T>();

        configure?.Invoke(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The DynamoDb saga repository configuration is invalid:");

        configurator.Repository(x => repositoryConfigurator.Register(x));

        return configurator;
    }

    /// <summary>Uses Amazon DynamoDB for compatible versioned sagas discovered and registered by runtime type.</summary>
    /// <param name="configurator">The service registration configurator to update.</param>
    /// <param name="configure">The callback applied to every compatible saga repository.</param>
    public static void SetDynamoDbSagaRepositoryProvider(this IRegistrationConfigurator configurator, Action<IDynamoDbSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        configurator.SetSagaRepositoryProvider(new DynamoDbSagaRepositoryRegistrationProvider(configure));
    }
}
