// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Services
{
    using System;
    using System.Threading.Tasks;
    using Contracts;


    public class ShakeMachine :
        IShakeMachine
    {
        public Task PourShake(string flavor, Size size)
        {
            if (flavor.Equals("strawberry", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ShakeMachineException("Strawberry is not available");
            }

            return Task.CompletedTask;
        }
    }


    public class ShakeMachineException :
        Exception
    {
        public ShakeMachineException(string message)
            : base(message)
        {
        }
    }
}
