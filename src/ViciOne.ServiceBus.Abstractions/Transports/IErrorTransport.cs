// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading.Tasks;


    /// <summary>
    /// If present, can be used to move the <see cref="ReceiveContext" /> to the error queue
    /// </summary>
    public interface IErrorTransport
    {
        Task Send(ExceptionReceiveContext context);
    }
}
