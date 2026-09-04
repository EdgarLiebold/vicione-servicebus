using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for publish pipeline configurator.
/// </summary>
public interface IPublishPipelineConfigurator
{
    /// <summary>
    /// Configure the Publish pipeline
    /// </summary>
    /// <param name="callback"></param>
    void ConfigurePublish(Action<IPublishPipeConfigurator> callback);
}
