// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Audit
{
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Audit;
    using Microsoft.EntityFrameworkCore;


    public class EntityFrameworkAuditStore :
        IMessageAuditStore
    {
        readonly string _auditTableName;
        readonly string _auditTableSchema;
        readonly DbContextOptions _contextOptions;

        public EntityFrameworkAuditStore(DbContextOptions contextOptions, string auditTableName, string auditTableSchema = null)
        {
            _contextOptions = contextOptions;
            _auditTableName = auditTableName;
            _auditTableSchema = auditTableSchema;
        }

        public DbContext AuditContext => new AuditDbContext(_contextOptions, _auditTableName, _auditTableSchema);

        async Task IMessageAuditStore.StoreMessage<T>(T message, MessageAuditMetadata metadata)
        {
            await using var dbContext = AuditContext;
            var auditRecord = AuditRecord.Create(message, TypeCache<T>.ShortName, metadata);

            await dbContext.Set<AuditRecord>().AddAsync(auditRecord).ConfigureAwait(false);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
