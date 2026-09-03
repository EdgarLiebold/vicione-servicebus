namespace ViciOne.ServiceBus.SqlTransport
{
    using System;
    using System.ComponentModel;


    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Defaults
    {
        public static TimeSpan LockDuration { get; } = TimeSpan.FromMinutes(5);
        public static TimeSpan DefaultMessageTimeToLive { get; } = TimeSpan.FromDays(365 + 1);
        public static TimeSpan ErrorQueueTimeToLive { get; } = TimeSpan.FromDays(14);

        public static TimeSpan AutoDeleteOnIdle { get; } = TimeSpan.FromDays(427);
        public static TimeSpan TemporaryAutoDeleteOnIdle { get; } = TimeSpan.FromMinutes(5);
        public static TimeSpan MaxAutoRenewDuration { get; } = TimeSpan.FromMinutes(5);

        public static TimeSpan SessionIdleTimeout { get; } = TimeSpan.FromSeconds(10);
        public static TimeSpan ShutdownTimeout { get; } = TimeSpan.FromMilliseconds(100);
    }
}
