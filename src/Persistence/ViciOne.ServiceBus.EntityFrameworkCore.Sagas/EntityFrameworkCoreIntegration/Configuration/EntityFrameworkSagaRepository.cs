using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Configuration;

sealed class EntityFrameworkSagaRepository :
    IEntityFrameworkSagaRepository
{
    readonly ConcurrentDictionary<Type, ISagaClassMap> _configurations;
    readonly DbContextOptions _dbContextOptions;

    public EntityFrameworkSagaRepository(DbContextOptions dbContextOptions)
    {
        _dbContextOptions = dbContextOptions ?? throw new ArgumentNullException(nameof(dbContextOptions));
        _configurations = new ConcurrentDictionary<Type, ISagaClassMap>();
    }

    public void AddSagaClassMap<TSaga>(ISagaClassMap<TSaga> sagaClassMap)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(sagaClassMap);

        ISagaClassMap registered = _configurations.GetOrAdd(sagaClassMap.SagaType, sagaClassMap);
        if (!ReferenceEquals(registered, sagaClassMap))
        {
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                "Entity Framework saga repository",
                "unknown",
                $"A mapping for saga type '{sagaClassMap.SagaType}' is already registered.",
                "Register exactly one mapping for each saga type"));
        }
    }

    public DbContext GetDbContext()
    {
        return new RepositorySagaDbContext(_dbContextOptions, _configurations.Values);
    }

    public static DbContextOptionsBuilder CreateOptionsBuilder()
    {
        return new DbContextOptionsBuilder<RepositorySagaDbContext>();
    }


    sealed class RepositorySagaDbContext : SagaDbContext
    {
        public RepositorySagaDbContext(DbContextOptions options, IEnumerable<ISagaClassMap> configurations)
            : base(options)
        {
            Configurations = configurations;
        }

        protected override IEnumerable<ISagaClassMap> Configurations { get; }
    }
}
