using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Configuration;

public sealed class JsonConversionContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "json-snapshot-detects-nested-mutation-and-persists-it")]
    public async Task JsonConversion_DetectsAnInPlaceMutationAndPersistsTheChangedValueAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<JsonDbContext>().UseSqlite(connection).Options;

        await using (var db = new JsonDbContext(options))
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            db.Add(new JsonEntity
            {
                Id = 1,
                Payload = new JsonPayload { Tenant = "north", Attempts = [1] },
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await using (var db = new JsonDbContext(options))
        {
            JsonEntity entity = await db.Set<JsonEntity>().SingleAsync(cancellationToken);
            PropertyEntry<JsonEntity, JsonPayload> property = db.Entry(entity).Property(x => x.Payload);
            JsonPayload original = property.OriginalValue;

            entity.Payload.Attempts.Add(2);
            db.ChangeTracker.DetectChanges();

            Assert.True(property.IsModified);
            Assert.NotSame(entity.Payload, original);
            Assert.Equal([1], original.Attempts);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using (var db = new JsonDbContext(options))
        {
            JsonEntity persisted = await db.Set<JsonEntity>().AsNoTracking().SingleAsync(cancellationToken);
            Assert.Equal("north", persisted.Payload.Tenant);
            Assert.Equal([1, 2], persisted.Payload.Attempts);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "json-comparer-keeps-equality-hash-and-snapshot-consistent")]
    public void JsonConversion_UsesStructuralEqualityHashAndIndependentSnapshots()
    {
        var options = new DbContextOptionsBuilder<JsonDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var db = new JsonDbContext(options);
        ValueComparer comparer = db.Model.FindEntityType(typeof(JsonEntity))!
            .FindProperty(nameof(JsonEntity.Payload))!
            .GetValueComparer()!;
        var left = new JsonPayload { Tenant = "north", Attempts = [1, 2] };
        var equal = new JsonPayload { Tenant = "north", Attempts = [1, 2] };
        var different = new JsonPayload { Tenant = "south", Attempts = [1, 2] };

        Assert.True(comparer.Equals(left, equal));
        Assert.Equal(comparer.GetHashCode(left), comparer.GetHashCode(equal));
        Assert.False(comparer.Equals(left, different));

        var snapshot = Assert.IsType<JsonPayload>(comparer.Snapshot(left));
        Assert.NotSame(left, snapshot);
        left.Attempts.Add(3);
        Assert.Equal([1, 2], snapshot.Attempts);
        Assert.False(comparer.Equals(left, snapshot));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "json-tracking-does-not-drop-fields-ignored-by-typed-equality")]
    public async Task JsonConversion_PersistsAJsonFieldIgnoredByTypedEqualityAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<JsonDbContext>().UseSqlite(connection).Options;

        await using (var db = new JsonDbContext(options))
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            db.Add(new JsonEntity
            {
                Id = 2,
                EquatablePayload = new SelectiveEquatablePayload { Tenant = "north", Revision = 1 },
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await using (var db = new JsonDbContext(options))
        {
            JsonEntity entity = await db.Set<JsonEntity>().SingleAsync(cancellationToken);
            PropertyEntry<JsonEntity, SelectiveEquatablePayload> property =
                db.Entry(entity).Property(x => x.EquatablePayload);
            entity.EquatablePayload.Revision = 2;
            db.ChangeTracker.DetectChanges();

            Assert.True(property.IsModified);
            Assert.Equal(1, property.OriginalValue.Revision);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using (var db = new JsonDbContext(options))
        {
            JsonEntity persisted = await db.Set<JsonEntity>().AsNoTracking().SingleAsync(cancellationToken);
            Assert.Equal(2, persisted.EquatablePayload.Revision);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-MAPPING", "json-tracking-does-not-trust-shallow-clone")]
    public async Task JsonConversion_PersistsNestedMutationDespiteAShallowCloneAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<JsonDbContext>().UseSqlite(connection).Options;

        await using (var db = new JsonDbContext(options))
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
            db.Add(new JsonEntity
            {
                Id = 3,
                CloneablePayload = new ShallowClonePayload { Attempts = [1] },
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        await using (var db = new JsonDbContext(options))
        {
            JsonEntity entity = await db.Set<JsonEntity>().SingleAsync(cancellationToken);
            PropertyEntry<JsonEntity, ShallowClonePayload> property =
                db.Entry(entity).Property(x => x.CloneablePayload);
            ShallowClonePayload original = property.OriginalValue;
            entity.CloneablePayload.Attempts.Add(2);
            db.ChangeTracker.DetectChanges();

            Assert.True(property.IsModified);
            Assert.NotSame(entity.CloneablePayload.Attempts, original.Attempts);
            Assert.Equal([1], original.Attempts);
            await db.SaveChangesAsync(cancellationToken);
        }

        await using (var db = new JsonDbContext(options))
        {
            JsonEntity persisted = await db.Set<JsonEntity>().AsNoTracking().SingleAsync(cancellationToken);
            Assert.Equal([1, 2], persisted.CloneablePayload.Attempts);
        }
    }

    public sealed class JsonEntity
    {
        public int Id { get; set; }
        public JsonPayload Payload { get; set; } = new();
        public SelectiveEquatablePayload EquatablePayload { get; set; } = new();
        public ShallowClonePayload CloneablePayload { get; set; } = new();
    }

    public sealed class JsonPayload
    {
        public string Tenant { get; set; } = string.Empty;
        public List<int> Attempts { get; set; } = [];
    }

    public sealed class SelectiveEquatablePayload : IEquatable<SelectiveEquatablePayload>
    {
        public string Tenant { get; set; } = string.Empty;
        public int Revision { get; set; }

        public bool Equals(SelectiveEquatablePayload? other) => other?.Tenant == Tenant;
        public override bool Equals(object? obj) => obj is SelectiveEquatablePayload other && Equals(other);
        public override int GetHashCode() => Tenant.GetHashCode(StringComparison.Ordinal);
    }

    public sealed class ShallowClonePayload : ICloneable
    {
        public List<int> Attempts { get; set; } = [];

        public object Clone() => MemberwiseClone();
    }

    private sealed class JsonDbContext(DbContextOptions<JsonDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JsonEntity>().Property(x => x.Payload).HasJsonConversion();
            modelBuilder.Entity<JsonEntity>().Property(x => x.EquatablePayload).HasJsonConversion();
            modelBuilder.Entity<JsonEntity>().Property(x => x.CloneablePayload).HasJsonConversion();
        }
    }
}
