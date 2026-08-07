// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System.Threading.Tasks;


    public interface IInactivityObserver
    {
        void Connected(IInactivityObservationSource source);

        Task NoActivity();

        void ForceInactive();
    }
}
