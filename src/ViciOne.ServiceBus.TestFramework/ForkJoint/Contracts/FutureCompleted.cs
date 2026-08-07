// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.ForkJoint.Contracts
{
    using System;


    public interface FutureCompleted
    {
        /// <summary>
        /// When the future was initially created
        /// </summary>
        DateTime Created { get; }

        /// <summary>
        /// When the future was finally completed
        /// </summary>
        DateTime Completed { get; }
    }
}
