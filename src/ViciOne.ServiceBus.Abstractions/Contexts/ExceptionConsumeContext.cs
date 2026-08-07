// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface ExceptionConsumeContext :
        ConsumeContext
    {
        /// <summary>
        /// The exception that was thrown
        /// </summary>
        Exception Exception { get; }

        /// <summary>
        /// The exception info, suitable for inclusion in a fault message
        /// </summary>
        ExceptionInfo ExceptionInfo { get; }
    }


    public interface ExceptionConsumeContext<out T> :
        ExceptionConsumeContext,
        ConsumeContext<T>
        where T : class
    {
    }
}
