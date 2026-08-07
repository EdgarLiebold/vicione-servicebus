// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Tests.Courier
{
    using MongoDB.Driver;
    using NUnit.Framework;
    using TestFramework;


    [TestFixture]
    public abstract class MongoDbTestFixture :
        InMemoryActivityTestFixture
    {
        protected const string EventCollectionName = "Events";
        protected const string EventDatabaseName = "EventStore";

        protected MongoClient Client;
        protected IMongoDatabase Database { get; set; }

        ViciOneServiceBusMongoDbConventions _convention;

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            base.ConfigureInMemoryBus(configurator);

            Client = new MongoClient("mongodb://127.0.0.1");

            Database = Client.GetDatabase(EventDatabaseName);

            _convention = new ViciOneServiceBusMongoDbConventions();
        }
    }
}
