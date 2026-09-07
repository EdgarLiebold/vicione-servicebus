using Microsoft.EntityFrameworkCore;
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

    private sealed class MappedSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }
}
