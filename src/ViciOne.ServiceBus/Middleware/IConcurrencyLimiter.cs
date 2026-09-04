using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;

namespace ViciOne.ServiceBus.Middleware;

public interface IConcurrencyLimiter :
    IConsumer<SetConcurrencyLimit>
{
    int Available { get; }
    int Limit { get; }

    Task Wait(CancellationToken cancellationToken);

    void Release();
}
