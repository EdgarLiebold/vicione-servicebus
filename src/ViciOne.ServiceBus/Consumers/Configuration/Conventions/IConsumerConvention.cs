// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    /// <summary>
    /// A consumer convention is used to find message types inside a consumer class.
    /// </summary>
    public interface IConsumerConvention
    {
        /// <summary>
        /// Returns the message convention for the type of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IConsumerMessageConvention GetConsumerMessageConvention<T>()
            where T : class;
    }
}
