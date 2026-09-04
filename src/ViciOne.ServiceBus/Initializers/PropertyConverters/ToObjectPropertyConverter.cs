using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyConverters;

public class ToObjectPropertyConverter<TInput> :
    IPropertyConverter<object, TInput>
{
    public Task<object> Convert<T>(InitializeContext<T> context, TInput input)
        where T : class
    {
        return Task.FromResult<object>(input);
    }
}
