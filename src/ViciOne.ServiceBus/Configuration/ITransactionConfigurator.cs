using System;
using System.Transactions;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures transaction.</summary>
public interface ITransactionConfigurator
{
    /// <summary>Sets the transaction timeout.</summary>
    TimeSpan Timeout { set; }

    /// <summary>Sets the isolation level of the transaction.</summary>
    IsolationLevel IsolationLevel { set; }
}
