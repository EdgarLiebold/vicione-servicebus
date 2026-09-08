using System;
using System.Collections.Generic;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Azure.Table.Configuration;

/// <summary>Collects and validates the Azure Table client and key strategy for one saga repository.</summary>
/// <typeparam name="TSaga">The saga state persisted by the repository.</typeparam>
internal sealed class AzureTableSagaRepositoryConfigurator<TSaga> :
    IAzureTableSagaRepositoryConfigurator,
    ISpecification
    where TSaga : class, ISaga
{
    Func<IServiceProvider, TableClient>? _tableClientFactory;

    IAzureTableSagaKeyFormatter _keyFormatter = new FixedPartitionSagaKeyFormatter(typeof(TSaga).Name);

    /// <summary>Uses a factory to resolve the Azure Table client.</summary>
    /// <param name="tableClientFactory">The factory invoked to obtain the Azure Table client.</param>
    public void UseTableClientFactory(Func<TableClient> tableClientFactory)
    {
        ArgumentNullException.ThrowIfNull(tableClientFactory);
        _tableClientFactory = provider => tableClientFactory();
    }

    /// <summary>Uses a service-provider-aware factory to resolve the Azure Table client.</summary>
    /// <param name="tableClientFactory">The factory that resolves an Azure Table client from the registration service provider.</param>
    public void UseTableClientFactory(Func<IServiceProvider, TableClient> tableClientFactory)
    {
        ArgumentNullException.ThrowIfNull(tableClientFactory);
        _tableClientFactory = tableClientFactory;
    }

    /// <summary>Uses the strategy that maps saga identifiers to Azure Table keys.</summary>
    /// <param name="keyFormatter">The saga-key formatting strategy.</param>
    public void UseKeyFormatter(IAzureTableSagaKeyFormatter keyFormatter)
    {
        ArgumentNullException.ThrowIfNull(keyFormatter);
        _keyFormatter = keyFormatter;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_tableClientFactory == null)
            yield return this.Failure("TableClientFactory", "must be specified");
    }

    /// <summary>Registers the validated Azure Table repository services for the saga type.</summary>
    /// <param name="configurator">The saga repository registration to update.</param>
    public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        Func<IServiceProvider, TableClient> tableClientFactory = _tableClientFactory
            ?? throw new InvalidOperationException("The Azure Table client factory must be configured before registration.");

        configurator.TryAddSingleton<IAzureTableClientProvider<TSaga>>(provider =>
            new FixedAzureTableClientProvider<TSaga>(tableClientFactory(provider)));
        configurator.TryAddSingleton(new AzureTableSagaKeyFormatterProvider<TSaga>(_keyFormatter));
        configurator.RegisterLoadSagaRepository<TSaga, AzureTableSagaRepositoryContextFactory<TSaga>>();
        configurator.RegisterSagaRepository<TSaga, IAzureTableSagaStorageContext<TSaga>, SagaConsumeContextFactory<IAzureTableSagaStorageContext<TSaga>, TSaga>,
            AzureTableSagaRepositoryContextFactory<TSaga>>();

        configurator.AddScoped(provider => new AzureTableSagaRepositoryContextFactory<TSaga>(
            provider.GetRequiredService<IAzureTableClientProvider<TSaga>>(),
            provider.GetRequiredService<ISagaConsumeContextFactory<IAzureTableSagaStorageContext<TSaga>, TSaga>>(),
            provider.GetRequiredService<AzureTableSagaKeyFormatterProvider<TSaga>>().Formatter));
        configurator.Replace(ServiceDescriptor.Scoped<ILoadSagaRepositoryContextFactory<TSaga>>(provider =>
            provider.GetRequiredService<AzureTableSagaRepositoryContextFactory<TSaga>>()));
        configurator.Replace(ServiceDescriptor.Scoped<ISagaRepositoryContextFactory<TSaga>>(provider =>
            provider.GetRequiredService<AzureTableSagaRepositoryContextFactory<TSaga>>()));
    }
}
