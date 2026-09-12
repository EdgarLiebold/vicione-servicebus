namespace ViciOne.ServiceBus.SignalR.Contracts;

/// <summary>Identifies the membership change requested from the node that owns a connection.</summary>
internal enum GroupCommandAction
{
    /// <summary>Adds the connection to the group.</summary>
    Add = 1,

    /// <summary>Removes the connection from the group.</summary>
    Remove = 2
}
