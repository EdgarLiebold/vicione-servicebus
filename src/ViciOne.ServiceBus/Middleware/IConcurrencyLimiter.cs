// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;


    public interface IConcurrencyLimiter :
        IConsumer<SetConcurrencyLimit>
    {
        int Available { get; }
        int Limit { get; }

        Task Wait(CancellationToken cancellationToken);

        void Release();
    }
}
