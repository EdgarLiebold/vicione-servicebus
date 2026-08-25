namespace ViciOne.ServiceBus
{
    using System;


    public interface ITimeoutConfigurator
    {
        /// <summary>
        /// The maximum time allowed for the configured operation. The value must be greater than zero.
        /// </summary>
        TimeSpan Timeout { set; }

        /// <summary>
        /// Overrides the context-scoped time provider for this timeout. When it is not set, the
        /// timeout uses the provider attached to each pipeline context.
        /// </summary>
        TimeProvider TimeProvider { set; }
    }
}
