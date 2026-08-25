namespace ViciOne.ServiceBus.Middleware
{
    using System.Threading.Tasks;


    /// <summary>
    /// Maintains the latest context to be passed through the filter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ILatestFilter<T>
        where T : class, PipeContext
    {
        /// <summary>
        /// The most recently observed context to pass through the filter. The task remains pending
        /// until the first context arrives and subsequently returns the current snapshot.
        /// </summary>
        Task<T> Latest { get; }
    }
}
