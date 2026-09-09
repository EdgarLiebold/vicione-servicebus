using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides access to middleware applied while messages are published.</summary>
public interface IPublishPipelineConfigurator
{
    /// <summary>Configures the publish pipeline.</summary>
    /// <param name="callback">The callback that adds publish middleware.</param>
    void ConfigurePublish(Action<IPublishPipeConfigurator> callback);
}
