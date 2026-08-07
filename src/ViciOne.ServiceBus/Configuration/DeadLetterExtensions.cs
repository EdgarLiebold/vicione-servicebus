// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using Configuration;


    public static class DeadLetterExtensions
    {
        /// <summary>
        /// Rescue exceptions via the alternate pipe
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="rescuePipe"></param>
        public static void UseDeadLetter(this IPipeConfigurator<ReceiveContext> configurator, IPipe<ReceiveContext> rescuePipe)
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            var rescueConfigurator = new DeadLetterPipeSpecification(rescuePipe);

            configurator.AddPipeSpecification(rescueConfigurator);
        }
    }
}
