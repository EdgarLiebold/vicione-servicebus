using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Configuration;

sealed class EntityFrameworkSagaRepository :
    IEntityFrameworkSagaRepository
{
    readonly Dictionary<Type, ISagaClassMap> _configurations;
    readonly object _configurationSync = new();
    readonly DbContextOptions _dbContextOptions;
    object _modelCacheIdentity = new();

    public EntityFrameworkSagaRepository(DbContextOptions dbContextOptions)
    {
        _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
        _configurations = new Dictionary<Type, ISagaClassMap>();
    }

    public void AddSagaClassMap<TSaga>(ISagaClassMap<TSaga> sagaClassMap)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(sagaClassMap);

        lock (_configurationSync)
        {
            if (_configurations.TryGetValue(sagaClassMap.SagaType, out ISagaClassMap? registered))
            {
                if (!ReferenceEquals(registered, sagaClassMap))
                {
                    throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                        "Entity Framework saga repository",
                        "unknown",
                        $"A mapping for saga type '{sagaClassMap.SagaType}' is already registered.",
                        "Register exactly one mapping for each saga type"));
                }

                return;
            }

            _configurations.Add(sagaClassMap.SagaType, sagaClassMap);
            _modelCacheIdentity = new object();
        }
    }

    public DbContext CreateDbContext()
    {
        lock (_configurationSync)
        {
            return new RepositorySagaDbContext(_dbContextOptions, _configurations.Values.ToArray(), _modelCacheIdentity);
        }
    }

    public static DbContextOptionsBuilder CreateOptionsBuilder()
    {
        return new DbContextOptionsBuilder<RepositorySagaDbContext>();
    }

    sealed class RepositorySagaDbContext : SagaDbContext
    {
        public RepositorySagaDbContext(DbContextOptions options, IReadOnlyList<ISagaClassMap> configurations, object modelCacheIdentity)
            : base(options)
        {
            Configurations = configurations;
            ModelCacheIdentity = modelCacheIdentity;
        }

        public object ModelCacheIdentity { get; }

        protected override IEnumerable<ISagaClassMap> Configurations { get; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, RepositorySagaModelCacheKeyFactory>();
        }
    }

    sealed class RepositorySagaModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
        {
            ArgumentNullException.ThrowIfNull(context);

            return context is RepositorySagaDbContext sagaContext
                ? (context.GetType(), sagaContext.ModelCacheIdentity, designTime)
                : (object)(context.GetType(), designTime);
        }

        public object Create(DbContext context) => Create(context, designTime: false);
    }
}
