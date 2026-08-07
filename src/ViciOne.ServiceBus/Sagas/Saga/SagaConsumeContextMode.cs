// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Saga
{
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
}
