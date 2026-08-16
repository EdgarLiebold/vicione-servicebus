namespace ViciOne.ServiceBus.Transactions
{
    using System.Threading.Tasks;


    public interface ITransactionalBus :
        IBus
    {
        Task Release();
    }
}
