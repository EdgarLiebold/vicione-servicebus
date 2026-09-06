namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a retry pipeline, including its policy and observers.</summary>
public interface IRetryConfigurator :
    IRetryPolicyConfigurator,
    IRetryObserverConnector
;
