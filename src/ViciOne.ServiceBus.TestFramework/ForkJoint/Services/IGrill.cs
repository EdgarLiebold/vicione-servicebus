// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Services
{
    using System.Threading.Tasks;
    using Contracts;


    public interface IGrill
    {
        Task<BurgerPatty> CookOrUseExistingPatty(decimal weight, bool cheese);
        void Add(BurgerPatty patty);
    }
}
