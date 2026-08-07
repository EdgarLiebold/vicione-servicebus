// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.QuartzIntegration.Tests
{
    using System;
    using System.Diagnostics;


    public static class Utils
    {
        public static TimeSpan Timeout
        {
            get
            {
                if (Debugger.IsAttached)
                    return TimeSpan.FromMinutes(10);

                return TimeSpan.FromSeconds(8);
            }
        }
    }
}
