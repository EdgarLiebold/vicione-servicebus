// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureTable.Saga
{
    using System.Collections.Generic;


    public interface IEntityPropertyConverter<in TEntity>
        where TEntity : class
    {
        void ToEntity(TEntity entity, IDictionary<string, object> entityProperties);
        void FromEntity(TEntity entity, IDictionary<string, object> entityProperties);
    }
}
