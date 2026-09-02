namespace ViciOne.ServiceBus.Middleware.Outbox
{
    using System.Threading;
    using System.Threading.Tasks;


    public interface IBusOutboxNotification<TScope>
        where TScope : class
    {
        Task WaitForDelivery(CancellationToken cancellationToken);
        void Delivered();
    }
}
