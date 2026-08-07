// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IPublishPipelineConfigurator
    {
        /// <summary>
        /// Configure the Publish pipeline
        /// </summary>
        /// <param name="callback"></param>
        void ConfigurePublish(Action<IPublishPipeConfigurator> callback);
    }
}
