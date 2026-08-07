// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// Used to bind additional exchanges to an already bound exchange
    /// </summary>
    public interface IRabbitMqExchangeToExchangeBindingConfigurator :
        IRabbitMqExchangeBindingConfigurator
    {
        /// <summary>
        /// Creates a binding with another exchange
        /// </summary>
        /// <param name="exchangeName">Exchange name of the new exchange</param>
        /// <param name="configure">Configuration for new exchange and how to bind to it</param>
        void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator> configure = null);
    }
}
