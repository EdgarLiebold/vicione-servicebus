using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViciOne.ServiceBus.EntityFrameworkCore.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Saga;

public sealed class SagaClassMapTests
{
    private const string CorrelationKeyAnnotation = "ViciOne:CorrelationKeyCustomization";

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "correlation-key-customization-is-applied")]
    public void Configure_AppliesTheCorrelationKeyCustomization()
    {
        DbContextOptions<MappingDbContext> options = new DbContextOptionsBuilder<MappingDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new MappingDbContext(options);

        IEntityType entity = context.Model.FindEntityType(typeof(MappedSaga))
            ?? throw new InvalidOperationException("The saga entity was not mapped.");
        IKey key = entity.FindPrimaryKey()
            ?? throw new InvalidOperationException("The saga correlation key was not mapped.");

        Assert.Equal([nameof(MappedSaga.CorrelationId)], key.Properties.Select(property => property.Name));
        Assert.Equal("applied", key.FindAnnotation(CorrelationKeyAnnotation)?.Value);
        Assert.Equal(ValueGenerated.Never, entity.FindProperty(nameof(MappedSaga.CorrelationId))?.ValueGenerated);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "missing-model-builder-is-rejected")]
    public void Configure_RejectsAMissingModelBuilder()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new CustomSagaMap().Configure(null!));

        Assert.Equal("model", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "one-mapping-owner-per-saga-type")]
    public void SharedRepository_RejectsASecondMappingOwnerForTheSameSagaType()
    {
        var repository = new EntityFrameworkSagaRepository(
            EntityFrameworkSagaRepository.CreateOptionsBuilder().UseSqlite("Data Source=:memory:").Options);
        var mapping = new CustomSagaMap();

        repository.AddSagaClassMap(mapping);
        repository.AddSagaClassMap(mapping);

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            repository.AddSagaClassMap(new CustomSagaMap()));

        Assert.Contains(typeof(MappedSaga).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one mapping", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "different-repositories-do-not-share-incompatible-models")]
    public void SharedRepository_DistinguishesTheMappingsOfSeparateRepositories()
    {
        DbContextOptions options = EntityFrameworkSagaRepository.CreateOptionsBuilder()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var northRepository = new EntityFrameworkSagaRepository(options);
        var southRepository = new EntityFrameworkSagaRepository(options);
        northRepository.AddSagaClassMap(new NamedSagaMap("NorthSagas"));
        southRepository.AddSagaClassMap(new NamedSagaMap("SouthSagas"));

        using DbContext north = northRepository.CreateDbContext();
        using DbContext south = southRepository.CreateDbContext();
        IEntityType northSaga = north.Model.FindEntityType(typeof(MappedSaga))!;
        IEntityType southSaga = south.Model.FindEntityType(typeof(MappedSaga))!;

        Assert.Equal("NorthSagas", northSaga.GetTableName());
        Assert.Equal("SouthSagas", southSaga.GetTableName());
        Assert.NotSame(north.Model, south.Model);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "late-map-registration-builds-a-new-model")]
    public void SharedRepository_BuildsANewModelAfterAddingAnotherSagaMap()
    {
        var repository = new EntityFrameworkSagaRepository(
            EntityFrameworkSagaRepository.CreateOptionsBuilder().UseSqlite("Data Source=:memory:").Options);
        repository.AddSagaClassMap(new CustomSagaMap());
        using DbContext before = repository.CreateDbContext();
        Assert.NotNull(before.Model.FindEntityType(typeof(MappedSaga)));
        Assert.Null(before.Model.FindEntityType(typeof(AdditionalSaga)));

        repository.AddSagaClassMap(new AdditionalSagaMap());
        using DbContext after = repository.CreateDbContext();

        Assert.NotNull(after.Model.FindEntityType(typeof(MappedSaga)));
        Assert.NotNull(after.Model.FindEntityType(typeof(AdditionalSaga)));
        Assert.Null(before.Model.FindEntityType(typeof(AdditionalSaga)));
        Assert.NotSame(before.Model, after.Model);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "unchanged-registration-reuses-cached-model")]
    public void SharedRepository_ReusesTheModelWhenItsMappingsHaveNotChanged()
    {
        var repository = new EntityFrameworkSagaRepository(
            EntityFrameworkSagaRepository.CreateOptionsBuilder().UseSqlite("Data Source=:memory:").Options);
        var mapping = new CustomSagaMap();
        repository.AddSagaClassMap(mapping);
        using DbContext first = repository.CreateDbContext();
        IModel firstModel = first.Model;

        repository.AddSagaClassMap(mapping);
        using DbContext second = repository.CreateDbContext();

        Assert.Same(firstModel, second.Model);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "model-cache-key-retains-design-time-identity")]
    public void SharedRepository_UsesAModelCacheKeyWithSeparateDesignTimeIdentity()
    {
        var repository = new EntityFrameworkSagaRepository(
            EntityFrameworkSagaRepository.CreateOptionsBuilder().UseSqlite("Data Source=:memory:").Options);
        repository.AddSagaClassMap(new CustomSagaMap());
        using DbContext context = repository.CreateDbContext();
        using DbContext sibling = repository.CreateDbContext();
        IModelCacheKeyFactory cacheKeys = context.GetService<IModelCacheKeyFactory>();

        Assert.Equal(cacheKeys.Create(context, designTime: false), cacheKeys.Create(sibling, designTime: false));
        Assert.NotEqual(cacheKeys.Create(context, designTime: false), cacheKeys.Create(context, designTime: true));
    }

    private sealed class MappingDbContext(DbContextOptions<MappingDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new CustomSagaMap().Configure(modelBuilder);
        }
    }

    private sealed class CustomSagaMap : SagaClassMap<MappedSaga>
    {
        protected override void ConfigureCorrelationIdKey(KeyBuilder keyBuilder)
        {
            keyBuilder.HasAnnotation(CorrelationKeyAnnotation, "applied");
        }
    }

    private sealed class NamedSagaMap(string tableName) : SagaClassMap<MappedSaga>
    {
        protected override void Configure(EntityTypeBuilder<MappedSaga> entity, ModelBuilder model)
        {
            entity.ToTable(tableName);
        }
    }

    private sealed class AdditionalSagaMap : SagaClassMap<AdditionalSaga>
    {
    }

    private sealed class MappedSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    private sealed class AdditionalSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
