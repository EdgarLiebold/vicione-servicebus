namespace ViciOne.ServiceBus.Configuration
{
    using Util;


    public class PublishTopologyConfigurationObservable :
        Connectable<IPublishTopologyConfigurationObserver>,
        IPublishTopologyConfigurationObserver
    {
        public void MessageTopologyCreated<T>(IMessagePublishTopologyConfigurator<T> configurator)
            where T : class
        {
            ForEach(observer => observer.MessageTopologyCreated(configurator));
        }
    }
}
