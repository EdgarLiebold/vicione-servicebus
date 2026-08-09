// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Internals;
    using ViciOne.ServiceBus.Tests;
    using ViciOne.ServiceBus.Tests.Saga.Messages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using NUnit.Framework;
    using TestFramework;
    using Transactions;


    /// <summary>
    /// This test fixture has nothing to do with the Saga, but I wanted to test EFCore with the TransactionOutbox,
    /// so this was the easiest project to add a test spec to which has EF Core already referenced.
    /// </summary>
    [TestFixture]
    public class TransactionalBusOutbox_Specs :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_not_publish_properly()
        {
            var message = new InitiateSimpleSaga();
            var product = new Product { Name = "Should_not_publish_properly" };
            var transactionOutbox = new TransactionalEnlistmentBus(Bus);

            await using (var dbContext = GetDbContext())
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                EntityEntry<Product> entity = dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();

                await transactionOutbox.Publish(message);
            }

            Assert.That(async () => await _received.OrTimeout(s: 3), Throws.TypeOf<TimeoutException>());

            await using (var dbContext = GetDbContext())
            {
                Assert.That(await dbContext.Products.AnyAsync(x => x.Id == product.Id), Is.False);
            }
        }

        [Test]
        public async Task Should_publish_after_db_create()
        {
            var message = new InitiateSimpleSaga();
            var product = new Product { Name = "Should_publish_after_db_create" };
            var transactionOutbox = new TransactionalEnlistmentBus(Bus);

            await using (var dbContext = GetDbContext())
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();

                await transactionOutbox.Publish(message);

                // Hasn't published yet
                Assert.That(async () => await _received.OrTimeout(s: 3), Throws.TypeOf<TimeoutException>());

                transaction.Complete();
            }

            // Now has published
            await _received;

            await using (var dbContext = GetDbContext())
            {
                Assert.That(await dbContext.Products.AnyAsync(x => x.Id == product.Id), Is.True);
            }
        }

        [Test]
        [Category("Flaky")]
        public async Task Should_publish_after_db_create_outbox_bus()
        {
            var message = new InitiateSimpleSaga();
            var product = new Product { Name = "Should_publish_after_db_create" };
            var bus = new TransactionalBus(Bus);

            await using (var dbContext = GetDbContext())
            {
                dbContext.Products.Add(product);
                await dbContext.SaveChangesAsync();

                await bus.Publish(message);

                // Hasn't published yet
                Assert.That(async () => await _received.OrTimeout(s: 3), Throws.TypeOf<TimeoutException>());
            }

            await bus.Release();

            // Now has published
            await _received;

            await using (var dbContext = GetDbContext())
            {
                Assert.That(await dbContext.Products.AnyAsync(x => x.Id == product.Id), Is.True);
            }
        }

        #pragma warning disable NUnit1032
        TaskCompletionSource<ConsumeContext<InitiateSimpleSaga>> _expectation;
        #pragma warning restore NUnit1032

        /// <summary>
        /// The expectation of the running test. All three tests here assert that the message has NOT
        /// arrived yet, so a single task created once during fixture set up made them order dependent:
        /// the first test completed it and every later one found it already completed and failed at
        /// once. Measured: each of them passes on its own, and the last one in the fixture failed after
        /// 19 ms. Re-arming per test removes the shared state; no assertion changes.
        /// </summary>
        Task<ConsumeContext<InitiateSimpleSaga>> _received => _expectation.Task;

        [SetUp]
        public void ArmTheExpectation()
        {
            _expectation = GetTask<ConsumeContext<InitiateSimpleSaga>>();
        }

        TransactionOutboxTestsDbContext GetDbContext()
        {
            var dbContext = new TransactionOutboxTestsDbContext(new DbContextOptionsBuilder()
                .UseSqlServer(LocalDbConnectionStringProvider.GetLocalDbConnectionString("ViciOneServiceBusUnitTests_TransactionOutbox")).Options);
            return dbContext;
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            // The endpoint is configured once for the whole fixture, so the handler completes whichever
            // expectation the running test armed rather than one task shared by all of them.
            configurator.Handler<InitiateSimpleSaga>(context =>
            {
                _expectation?.TrySetResult(context);
                return Task.CompletedTask;
            });
        }

        public TransactionalBusOutbox_Specs()
        {
            using (var dbContext = GetDbContext())
            {
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                //RelationalDatabaseCreator databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
                //databaseCreator.CreateTables();
            }
        }
    }


    public class TransactionOutboxTestsDbContext :
        DbContext
    {
        public TransactionOutboxTestsDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
    }


    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
    }
}
