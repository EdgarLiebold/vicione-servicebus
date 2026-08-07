// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureTable
{
    using System.Collections.Generic;


    public interface IEntityConverter<T>
        where T : class
    {
        IDictionary<string, object> GetDictionary(T entity);
        T GetObject(IDictionary<string, object> entityProperties);
    }
}
