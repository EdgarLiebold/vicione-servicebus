using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures saga persistence backed by Azure Service Bus sessions.</summary>
public static class MessageSessionSagaRepositoryConfigurationExtensions
{
    /// <summary>Configures the saga to use the Azure Service Bus session for saga persistence.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="configurator">The saga registration to configure.</param>
    /// <returns>The same registration configurator for fluent chaining.</returns>
    public static ISagaRegistrationConfigurator<T> MessageSessionRepository<T>(this ISagaRegistrationConfigurator<T> configurator)
        where T : class, ISaga
    {
        configurator.Repository(x => x.RegisterSagaRepository<T, MessageSessionContext, SagaConsumeContextFactory<MessageSessionContext, T>,
            MessageSessionSagaRepositoryContextFactory<T>>());

        return configurator;
    }

    /// <summary>Uses Azure Service Bus session state for sagas registered without a type-specific repository call.</summary>
    /// <param name="configurator">The registration collection to configure.</param>
    public static void SetMessageSessionSagaRepositoryProvider(this IRegistrationConfigurator configurator)
    {
        configurator.SetSagaRepositoryProvider(new MessageSessionSagaRepositoryRegistrationProvider());
    }
}
