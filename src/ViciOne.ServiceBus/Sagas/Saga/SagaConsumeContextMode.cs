namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Specifies the available saga consume context mode values.
/// </summary>
public enum SagaConsumeContextMode
{
    /// <summary>
    /// Existing saga loaded from storage
    /// </summary>
    Load = 0,

    /// <summary>
    /// New saga created
    /// </summary>
    Add = 1,

    /// <summary>
    /// New saga inserted prior to event
    /// </summary>
    Insert = 2
}
