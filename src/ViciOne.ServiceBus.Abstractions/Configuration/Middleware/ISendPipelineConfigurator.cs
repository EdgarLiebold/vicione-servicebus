// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface ISendPipelineConfigurator
    {
        /// <summary>
        /// Configure the Send pipeline
        /// </summary>
        /// <param name="callback"></param>
        void ConfigureSend(Action<ISendPipeConfigurator> callback);
    }
}
