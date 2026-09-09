namespace ViciOne.ServiceBus.Operations;

/// <summary>Typed, idempotent outcome returned by reliable-messaging operator actions.</summary>
public sealed record ReliableMessagingOperationResult
{
    /// <summary>Gets the maximum length of an operational state name.</summary>
    public const int MaximumStateNameCharacters = 128;

    /// <summary>Creates a validated operator result.</summary>
    /// <param name="reference">The targeted outbox or inbox identity.</param>
    /// <param name="disposition">Whether the requested transition was applied.</param>
    /// <param name="previousState">The stable previous-state name, or <see langword="null" /> when the record was not found.</param>
    /// <param name="currentState">The stable resulting-state name, or <see langword="null" /> when the record was not found.</param>
    /// <exception cref="ArgumentException">The reference, disposition, or state-name combination is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A state name exceeds the published bound.</exception>
    public ReliableMessagingOperationResult(
        ReliableMessageReference reference,
        ReliableMessagingOperationDisposition disposition,
        string? previousState,
        string? currentState)
    {
        Reference = reference.Validate();
        if (!Enum.IsDefined(disposition))
            throw new ArgumentException("The reliable-messaging operation disposition is undefined.", nameof(disposition));
        if (disposition == ReliableMessagingOperationDisposition.NotFound)
        {
            if (previousState is not null || currentState is not null)
            {
                throw new ArgumentException(
                    "A not-found result cannot report retained states.",
                    previousState is not null ? nameof(previousState) : nameof(currentState));
            }
        }
        else
        {
            ValidateStateName(previousState, nameof(previousState));
            ValidateStateName(currentState, nameof(currentState));
        }

        Disposition = disposition;
        PreviousState = previousState;
        CurrentState = currentState;
    }

    /// <summary>Gets the targeted outbox or inbox identity.</summary>
    public ReliableMessageReference Reference { get; }

    /// <summary>Gets whether the requested transition was applied.</summary>
    public ReliableMessagingOperationDisposition Disposition { get; }

    /// <summary>Gets the stable previous-state name, or <see langword="null" /> when the record was not found.</summary>
    public string? PreviousState { get; }

    /// <summary>Gets the stable resulting-state name, or <see langword="null" /> when the record was not found.</summary>
    public string? CurrentState { get; }

    /// <summary>Gets whether the requested state transition was applied.</summary>
    public bool IsApplied => Disposition == ReliableMessagingOperationDisposition.Applied;

    static void ValidateStateName(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl))
            throw new ArgumentException("An operation state name cannot be empty or contain control characters.", parameterName);
        if (value.Length > MaximumStateNameCharacters)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value.Length,
                $"An operation state name cannot exceed {MaximumStateNameCharacters} characters.");
        }
    }
}
