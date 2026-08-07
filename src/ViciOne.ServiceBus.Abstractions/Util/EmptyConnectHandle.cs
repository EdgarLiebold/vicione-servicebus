// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Util
{
    /// <summary>
    /// A do-nothing connect handle, simply to satisfy
    /// </summary>
    public class EmptyConnectHandle :
        ConnectHandle
    {
        public void Dispose()
        {
        }

        public void Disconnect()
        {
        }
    }
}
