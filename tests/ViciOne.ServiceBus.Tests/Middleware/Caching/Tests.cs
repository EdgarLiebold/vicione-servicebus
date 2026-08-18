namespace ViciOne.ServiceBus.Tests.Middleware.Caching
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Caching;
    using NUnit.Framework;


    [TestFixture]
    public class Adding_a_value_directly_to_the_cache
    {
        [Test]
        public async Task Should_return_the_added_value_without_asking_the_factory()
        {
            var settings = new CacheSettings(100, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));
            var cache = new GreenCache<Endpoint>(settings);

            IIndex<Uri, Endpoint> addressIndex = cache.AddIndex("address", x => x.Address);

            var address = new Uri("rabbitmq://localhost/vhost/input-queue");

            var added = new Endpoint { Address = address };
            cache.Add(added);

            var factoryCalls = 0;

            var endpoint = await addressIndex.Get(address, key =>
            {
                Interlocked.Increment(ref factoryCalls);

                return Task.FromResult(new Endpoint { Address = key });
            });

            Assert.Multiple(() =>
            {
                Assert.That(endpoint, Is.SameAs(added), "the index returned a different instance than the one that was added");
                Assert.That(factoryCalls, Is.Zero, "the factory was asked for a value the cache already held");
                Assert.That(cache.Statistics.Count, Is.EqualTo(1), "the directly added value is not counted as one held value");
            });
        }
    }


    namespace TestValueObjects
    {
        using System;
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
}
