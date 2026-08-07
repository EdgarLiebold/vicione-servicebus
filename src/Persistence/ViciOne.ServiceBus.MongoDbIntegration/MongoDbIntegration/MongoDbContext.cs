// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.MongoDbIntegration
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using MongoDB.Driver;


    public interface MongoDbContext :
        IDisposable
    {
        /// <summary>
        /// The currently active session, if so started
        /// </summary>
        IClientSessionHandle? Session { get; }

        Guid? TransactionId { get; }

        Task<IClientSessionHandle> StartSession(CancellationToken cancellationToken);

        Task BeginTransaction(CancellationToken cancellationToken);
        Task CommitTransaction(CancellationToken cancellationToken);
        Task AbortTransaction(CancellationToken cancellationToken);

        MongoDbCollectionContext<T> GetCollection<T>();
    }
}
