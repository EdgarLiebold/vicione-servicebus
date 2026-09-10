namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Specifies how an in-memory exchange routes messages to its bound destinations.</summary>
public enum InMemoryExchangeType
{
    /// <summary>Routes each message to every bound destination.</summary>
    FanOut = 0,

    /// <summary>Routes a message to destinations bound to its exact routing key.</summary>
    Direct = 1,

    /// <summary>Routes a message to destinations whose topic patterns match its routing key.</summary>
    Topic = 2,
}
