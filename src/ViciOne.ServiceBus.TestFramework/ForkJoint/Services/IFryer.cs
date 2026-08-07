// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Services
{
    using System.Threading.Tasks;
    using Contracts;


    public interface IFryer
    {
        Task CookOnionRings(int quantity);

        Task CookFry(Size size);
    }
}
