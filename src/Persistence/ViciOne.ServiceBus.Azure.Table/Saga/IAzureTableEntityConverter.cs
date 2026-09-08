using System.Collections.Generic;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

internal interface IAzureTableEntityConverter<T>
    where T : class
{
    IDictionary<string, object> GetDictionary(T entity);

    T GetObject(IDictionary<string, object> entityProperties);
}
