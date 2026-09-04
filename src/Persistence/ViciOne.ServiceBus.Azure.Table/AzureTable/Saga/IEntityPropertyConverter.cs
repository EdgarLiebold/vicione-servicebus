using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureTable.Saga;

public interface IEntityPropertyConverter<in TEntity>
    where TEntity : class
{
    void ToEntity(TEntity entity, IDictionary<string, object> entityProperties);
    void FromEntity(TEntity entity, IDictionary<string, object> entityProperties);
}
