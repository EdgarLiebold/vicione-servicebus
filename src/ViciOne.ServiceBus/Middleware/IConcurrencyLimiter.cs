using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware;

public interface IConcurrencyLimiter :
    IConsumer<SetConcurrencyLimit>
{
    int Available { get; }
    int Limit { get; }

    Task WaitAsync(CancellationToken cancellationToken);

    void Release();
}
