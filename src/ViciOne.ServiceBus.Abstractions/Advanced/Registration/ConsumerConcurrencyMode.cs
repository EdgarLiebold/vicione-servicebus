namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Defines how a consumer admits concurrent message deliveries.</summary>
public enum ConsumerConcurrencyMode
{
    /// <summary>Admits deliveries independently up to the configured concurrency limit.</summary>
    Parallel = 0,

    /// <summary>Admits one delivery at a time.</summary>
    Serial = 1,

    /// <summary>Serializes deliveries within each selected partition while allowing partitions to run concurrently.</summary>
    Partitioned = 2
}
