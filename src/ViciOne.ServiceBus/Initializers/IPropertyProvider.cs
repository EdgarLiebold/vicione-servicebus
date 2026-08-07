// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers
{
    using System.Threading.Tasks;


    /// <summary>
    /// Returns the property from the input
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TProperty"></typeparam>
    public interface IPropertyProvider<in TInput, TProperty>
        where TInput : class
    {
        Task<TProperty> GetProperty<T>(InitializeContext<T, TInput> context)
            where T : class;
    }
}
