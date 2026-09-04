using System;
using System.Collections.Generic;

#nullable enable
namespace ViciOne.ServiceBus.Testing.Implementations;

sealed class TestObservationList<T>
{
    readonly Queue<T> _items = new();
    readonly int _maximumSavedElements;
    readonly TestContextSaveMode _saveMode;

    public TestObservationList(TestContextSaveMode saveMode, int maximumSavedElements)
    {
        if (maximumSavedElements <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumSavedElements));

        _saveMode = saveMode;
        _maximumSavedElements = maximumSavedElements;
    }

    public void Add(T item)
    {
        if (_saveMode == TestContextSaveMode.None)
            return;

        lock (_items)
        {
            if (_saveMode == TestContextSaveMode.Bounded && _items.Count == _maximumSavedElements)
                _items.Dequeue();

            _items.Enqueue(item);
        }
    }

    public IReadOnlyList<T> Snapshot()
    {
        lock (_items)
            return _items.ToArray();
    }
}
