// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration
{
    using MongoDB.Driver;


    public interface ICollectionNameFormatter
    {
        string Saga<TSaga>()
            where TSaga : ISaga;

        string Collection<T>()
            where T : class;
    }


    public static class CollectionNameFormatterExtensions
    {
        public static IMongoCollection<TSaga> GetCollection<TSaga>(this IMongoDatabase database, ICollectionNameFormatter collectionNameFormatter)
            where TSaga : class, ISaga
        {
            return database.GetCollection<TSaga>(collectionNameFormatter.Saga<TSaga>());
        }
    }
}
