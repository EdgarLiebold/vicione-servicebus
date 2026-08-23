namespace ViciOne.ServiceBus.Tests.Middleware.Caching.TestValueObjects
{
    using System;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Caching;


    public class SimpleValue
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }


    public class SmartValue :
        INotifyValueUsed,
        IAsyncDisposable
    {
        readonly string _id;
        readonly string _value;

        public SmartValue(string id, string value)
        {
            _id = id;
            _value = value;
        }

        public string Id => _id;

        public string Value
        {
            get
            {
                Used?.Invoke();

                return _value;
            }
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public event Action Used;
    }


    // The factories yield so that the cache sees a value that is not already there when it is asked
    // for. A delay would add wall clock time to every one of the hundreds of values these fixtures
    // create without making the result any less complete.
    public static class SmartValueFactory
    {
        public static async Task<SmartValue> Healthy(string id)
        {
            await Task.Yield();

            return new SmartValue(id, $"The key is {id}");
        }
    }


    public static class SimpleValueFactory
    {
        public static async Task<SimpleValue> Healthy(string id)
        {
            await Task.Yield();

            return new SimpleValue
            {
                Id = id,
                Value = $"The key is {id}"
            };
        }

        public static async Task<SimpleValue> Faulty(string id)
        {
            await Task.Yield();

            throw new TestException("The SimpleValue factory is quite faulty at the moment.");
        }
    }
}
