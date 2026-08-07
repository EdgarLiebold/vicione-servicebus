// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers.PropertyConverters
{
    using System.Threading.Tasks;


    public class ToObjectPropertyConverter<TInput> :
        IPropertyConverter<object, TInput>
    {
        public Task<object> Convert<T>(InitializeContext<T> context, TInput input)
            where T : class
        {
            return Task.FromResult<object>(input);
        }
    }
}
