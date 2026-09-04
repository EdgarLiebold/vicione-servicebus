using System.Collections.Generic;

namespace ViciOne.ServiceBus.AzureTable;

public interface IEntityConverter<T>
    where T : class
{
    IDictionary<string, object> GetDictionary(T entity);
    T GetObject(IDictionary<string, object> entityProperties);
}
