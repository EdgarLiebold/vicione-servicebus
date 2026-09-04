using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for send pipeline configurator.
/// </summary>
public interface ISendPipelineConfigurator
{
    /// <summary>
    /// Configure the Send pipeline
    /// </summary>
    /// <param name="callback"></param>
    void ConfigureSend(Action<ISendPipeConfigurator> callback);
}
