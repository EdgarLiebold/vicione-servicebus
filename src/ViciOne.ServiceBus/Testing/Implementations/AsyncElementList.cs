namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Util;


    public abstract class AsyncElementList<TElement> :
        IAsyncElementList<TElement>
        where TElement : class, IAsyncListElement
    {
        readonly Connectable<Channel<TElement>> _channels;
        readonly IDictionary<Guid, TElement> _messageLookup;
        readonly List<TElement> _messages;
        readonly CancellationToken _testCompleted;
        readonly TimeProvider _timeProvider;
        readonly TimeSpan _timeout;

        protected AsyncElementList(TimeSpan timeout, CancellationToken testCompleted = default)
            : this(timeout, testCompleted, TimeProvider.System)
        {
        }

        protected AsyncElementList(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        {
            _timeout = timeout;
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _testCompleted = testCompleted;

            _messages = new List<TElement>();
            _messageLookup = new Dictionary<Guid, TElement>();
            _channels = new Connectable<Channel<TElement>>();
        }

        protected TimeProvider TimeProvider => _timeProvider;

        public async IAsyncEnumerable<TElement> SelectAsync(FilterDelegate<TElement> filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<TElement>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

            var handle = _channels.Connect(channel);

            var returned = new HashSet<Guid>();

            try
            {
                var index = 0;

                TElement[] messages;
                lock (_messages)
                    messages = _messages.ToArray();

                for (; index < messages.Length; index++)
                {
                    var entry = messages[index];
                    var elementId = entry.ElementId.Value;

                    if (filter(entry) && !returned.Contains(elementId))
                    {
                        returned.Add(elementId);
                        yield return entry;
                    }
                }

                if (cancellationToken.IsCancellationRequested || _testCompleted.IsCancellationRequested)
                    yield break;

                using var timeout = new CancellationTokenSource(_timeout, _timeProvider);

                using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _testCompleted, cancellationToken);

                while (!linked.IsCancellationRequested)
                {
                    try
                    {
                        await channel.Reader.WaitToReadAsync(linked.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (!channel.Reader.TryRead(out _))
                        break;

                    lock (_messages)
                        messages = _messages.ToArray();

                    for (; index < messages.Length; index++)
                    {
                        var entry = messages[index];
                        var elementId = entry.ElementId.Value;

                        if (filter(entry) && !returned.Contains(elementId))
                        {
                            returned.Add(elementId);
                            yield return entry;
                        }
                    }
                }
            }
            finally
            {
                handle.Disconnect();
                channel.Writer.TryComplete();
            }
        }

        public async Task<bool> Any(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default)
        {
            try
            {
                await foreach (var _ in SelectAsync(filter, cancellationToken).ConfigureAwait(false))
                    return true;
            }
            catch (OperationCanceledException)
            {
            }

            return false;
        }

        public IEnumerable<TElement> Select(FilterDelegate<TElement> filter, CancellationToken cancellationToken = default)
        {
            var index = 0;

            TElement[] messages;
            lock (_messages)
                messages = _messages.ToArray();

            for (; index < messages.Length; index++)
            {
                var entry = messages[index];
                if (filter(entry))
                    yield return entry;
            }

            if (cancellationToken.IsCancellationRequested || _testCompleted.IsCancellationRequested)
                yield break;

            using var timeout = new CancellationTokenSource(_timeout, _timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _testCompleted, cancellationToken);

            using var cancellationRegistration = linked.Token.Register(static state =>
            {
                var messageList = (List<TElement>)state;
                lock (messageList)
                {
                    Monitor.PulseAll(messageList);
                }
            }, _messages);

            while (true)
            {
                lock (_messages)
                {
                    while (index == _messages.Count && !linked.IsCancellationRequested)
                        Monitor.Wait(_messages);

                    if (index == _messages.Count)
                        yield break;

                    messages = _messages.GetRange(index, _messages.Count - index).ToArray();
                    index += messages.Length;
                }

                foreach (var element in messages)
                {
                    if (filter(element))
                        yield return element;
                }
            }
        }

        protected void Add(TElement context)
        {
            if (!context.ElementId.HasValue)
                return;

            lock (_messages)
            {
                var elementId = context.ElementId.Value;

                if (_messageLookup.ContainsKey(elementId))
                    return;

                _messages.Add(context);
                _messageLookup.Add(elementId, context);

                Monitor.PulseAll(_messages);
            }

            _channels.ForEach(channel => channel.Writer.TryWrite(context));
        }
    }
}
