using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

public interface IInactivityObserver
{
    void Connected(IInactivityObservationSource source);

    Task NoActivityAsync(CancellationToken cancellationToken = default);

    void ForceInactive();
}
