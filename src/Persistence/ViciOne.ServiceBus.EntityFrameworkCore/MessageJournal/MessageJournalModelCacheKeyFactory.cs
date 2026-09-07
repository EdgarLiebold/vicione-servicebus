using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;

internal sealed class MessageJournalModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context is MessageJournalDbContext journalContext
            ? (context.GetType(), journalContext.TableName, journalContext.SchemaName, designTime)
            : (object)(context.GetType(), designTime);
    }

    public object Create(DbContext context) => Create(context, designTime: false);
}
