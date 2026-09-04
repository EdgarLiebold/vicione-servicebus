using System;
using System.Collections.Generic;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.AzureTable;
using ViciOne.ServiceBus.AzureTable.Saga;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

public class AzureTableSagaRepositoryConfigurator<TSaga> :
    IAzureTableSagaRepositoryConfigurator<TSaga>,
    ISpecification
    where TSaga : class, ISaga
{
    Func<IServiceProvider, TableClient>? _tableClientFactory;

    Func<IServiceProvider, ISagaKeyFormatter<TSaga>> _formatterFactory = provider =>
        new ConstPartitionSagaKeyFormatter<TSaga>(typeof(TSaga).Name);

    /// <summary>
    /// Supply a factory for retrieving the Azure Data Tables client.
    /// </summary>
    /// <param name="tableClientFactory">The table-client factory.</param>
    public void TableClientFactory(Func<TableClient> tableClientFactory)
    {
        ArgumentNullException.ThrowIfNull(tableClientFactory);
        _tableClientFactory = provider => tableClientFactory();
    }

    /// <summary>
    /// Supply a service-provider-aware factory for retrieving the Azure Data Tables client.
    /// </summary>
    /// <param name="tableClientFactory">The table-client factory.</param>
    public void TableClientFactory(Func<IServiceProvider, TableClient> tableClientFactory)
    {
        ArgumentNullException.ThrowIfNull(tableClientFactory);
        _tableClientFactory = tableClientFactory;
    }

    /// <summary>
    /// Supply factory for retrieving the key formatter.
    /// </summary>
    /// <param name="formatterFactory"></param>
    public void KeyFormatter(Func<ISagaKeyFormatter<TSaga>> formatterFactory)
    {
        ArgumentNullException.ThrowIfNull(formatterFactory);
        _formatterFactory = provider => formatterFactory();
    }

    public IEnumerable<ValidationResult> Validate()
    {
        if (_tableClientFactory == null)
            yield return this.Failure("TableClientFactory", "must be specified");
    }

    public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        Func<IServiceProvider, TableClient> tableClientFactory = _tableClientFactory
            ?? throw new InvalidOperationException("The Azure Table client factory must be configured before registration.");

        configurator.TryAddSingleton<ITableClientProvider<TSaga>>(provider =>
            new FixedTableClientProvider<TSaga>(tableClientFactory(provider)));
        configurator.TryAddSingleton(_formatterFactory);
        configurator.RegisterLoadSagaRepository<TSaga, AzureTableSagaRepositoryContextFactory<TSaga>>();
        configurator.RegisterSagaRepository<TSaga, DatabaseContext<TSaga>, SagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>,
            AzureTableSagaRepositoryContextFactory<TSaga>>();

        // The provider stays assembly-internal so the public API exposes only TableClient vocabulary.
        // Register the factory explicitly because the default DI activator considers public constructors
        // only and would otherwise try to resolve the public TableClient constructor directly.
        configurator.AddScoped(provider => new AzureTableSagaRepositoryContextFactory<TSaga>(
            provider.GetRequiredService<ITableClientProvider<TSaga>>(),
            provider.GetRequiredService<ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga>>(),
            provider.GetRequiredService<ISagaKeyFormatter<TSaga>>()));
        configurator.Replace(ServiceDescriptor.Scoped<ILoadSagaRepositoryContextFactory<TSaga>>(provider =>
            provider.GetRequiredService<AzureTableSagaRepositoryContextFactory<TSaga>>()));
        configurator.Replace(ServiceDescriptor.Scoped<ISagaRepositoryContextFactory<TSaga>>(provider =>
            provider.GetRequiredService<AzureTableSagaRepositoryContextFactory<TSaga>>()));
    }
}
