namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes one result produced while validating configuration before resources are allocated.</summary>
public interface ValidationResult
{
    /// <summary>Gets the severity that determines whether configuration may continue.</summary>
    ValidationResultDisposition Disposition { get; }

    /// <summary>Gets the human-readable validation message.</summary>
    string Message { get; }

    /// <summary>Gets the dotted path of the configuration member associated with the result.</summary>
    string Key { get; }

    /// <summary>Gets the rejected or noteworthy value, when one is available.</summary>
    string? Value { get; }
}
