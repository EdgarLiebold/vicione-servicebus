// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    public class ExecuteActivityReceiveEndpointDispatcher<TActivity, TArguments> :
        ITypeReceiveEndpointDispatcherFactory
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        public IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
        {
            var queueName = formatter.ExecuteActivity<TActivity, TArguments>();

            return factory.CreateExecuteActivityReceiver<TActivity>(queueName);
        }
    }
}
