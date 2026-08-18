namespace ViciOne.ServiceBus.Tests.Middleware.Caching
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Caching;
    using NUnit.Framework;
    using TestValueObjects;


    [TestFixture]
    public class Using_an_index_to_add_an_item
    {
        [Test]
        public async Task Should_not_find_a_faulted_value()
        {
            IIndex<string, SimpleValue> index = new GreenCache<SimpleValue>().AddIndex("id", x => x.Id);

            var helloKey = "Hello";

            Task<SimpleValue> valueTask = index.Get(helloKey, SimpleValueFactory.Faulty);

            Assert.That(async () => await valueTask, Throws.TypeOf<TestException>());

            Assert.That(async () => await index.Get(helloKey), Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public async Task Should_support_a_simple_addition()
        {
            IIndex<string, SimpleValue> index = new GreenCache<SimpleValue>().AddIndex("id", x => x.Id);

            var helloKey = "Hello";

            var value = await index.Get(helloKey, SimpleValueFactory.Healthy);

            Assert.That(value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(value.Id, Is.EqualTo(helloKey));
                Assert.That(value.Value, Is.EqualTo("The key is Hello"));
            });
        }

        [Test]
        public async Task Should_support_a_simple_addition_and_access()
        {
            IIndex<string, SimpleValue> index = new GreenCache<SimpleValue>().AddIndex("id", x => x.Id);

            var helloKey = "Hello";

            var value = await index.Get(helloKey, SimpleValueFactory.Healthy);

            Task<SimpleValue> readValueTask = index.Get(helloKey);

            Assert.That(value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(value.Id, Is.EqualTo(helloKey));
                Assert.That(value.Value, Is.EqualTo("The key is Hello"));
            });

            var readValue = await readValueTask;

            Assert.That(readValue, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(readValue.Id, Is.EqualTo(helloKey));
                Assert.That(readValue.Value, Is.EqualTo("The key is Hello"));
            });
        }

        [Test]
        public async Task Should_support_access_to_eventual_success()
        {
            IIndex<string, SimpleValue> index = new GreenCache<SimpleValue>().AddIndex("id", x => x.Id);

            var helloKey = "Hello";

            // The second request and the plain read have to arrive while the first value is still pending.
            // A factory the test releases itself states that; a factory that merely takes a while leaves it
            // to whichever continuation runs first.
            var pending = new ControlledValueFactory();

            Task<SimpleValue> valueTask = index.Get(helloKey, pending.Create);

            await pending.Started;

            Task<SimpleValue> goodValueTask = index.Get(helloKey, SimpleValueFactory.Healthy);

            Task<SimpleValue> readValueTask = index.Get(helloKey);

            pending.Fail(new TestException("The SimpleValue factory is quite faulty at the moment."));

            Assert.That(async () => await valueTask, Throws.TypeOf<TestException>());

            var value = await goodValueTask;

            Assert.That(value, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(value.Id, Is.EqualTo(helloKey));
                Assert.That(value.Value, Is.EqualTo("The key is Hello"));
            });

            var readValue = await readValueTask;

            Assert.That(readValue, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(readValue.Id, Is.EqualTo(helloKey));
                Assert.That(readValue.Value, Is.EqualTo("The key is Hello"));
            });
        }


        /// <summary>
        /// A value factory whose start and outcome the test decides, so a fixture can state that a value is
        /// still pending instead of hoping that it is.
        /// </summary>
        class ControlledValueFactory
        {
            readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            readonly TaskCompletionSource<SimpleValue> _value = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task<bool> Started => _started.Task;

            public Task<SimpleValue> Create(string key)
            {
                _started.TrySetResult(true);

                return _value.Task;
            }

            public void Fail(Exception exception)
            {
                _value.TrySetException(exception);
            }
        }
    }
}
