using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

public interface ICorrelationIdSelector<T>
    where T : class
{
    bool TryGetSetCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId);
}
