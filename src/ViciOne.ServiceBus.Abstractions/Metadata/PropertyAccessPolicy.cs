namespace ViciOne.ServiceBus.Metadata;


/// <summary>Defines which property accessors may be used when ViciOne.ServiceBus builds cached metadata.</summary>
public enum PropertyAccessPolicy
{
    /// <summary>Only public accessors are eligible.</summary>
    PublicOnly = 0,

    /// <summary>Public and non-public accessors are eligible.</summary>
    IncludeNonPublic = 1
}
