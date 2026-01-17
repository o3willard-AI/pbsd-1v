using System;
using System.Collections.Generic;

namespace PairAdmin.DataStructures;

/// <summary>
/// Circular buffer for storing terminal output with fixed capacity
/// Thread-safe implementation using lock for thread safety
/// </summary>
/// <typeparam name="T">Type of items in the buffer</typeparam>
public class CircularBuffer<T> : IDisposable
{
    private readonly T[] _buffer;
    private readonly int _capacity;
    private int _head;
    private int _tail;
    private int _count;
    private readonly object _syncLock = new object();

    /// <summary>
    /// Initializes a new instance of CircularBuffer with specified capacity
    /// </summary>
    /// <param name="capacity">Maximum number of items the buffer can hold</param>
    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");
        }

        _buffer = new T[capacity];
        _capacity = capacity;
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    /// <summary>
    /// Initializes a new instance of CircularBuffer with default capacity
    /// </summary>
    public CircularBuffer() : this(100)
    {
    }

    /// <summary>
    /// Gets the current number of items in the buffer
    /// </summary>
    public int Count
    {
        get
        {
            lock (_syncLock)
            {
                return _count;
            }
        }
    }

    /// <summary>
    /// Gets the capacity of the buffer
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets whether the buffer is empty
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            lock (_syncLock)
            {
                return _count == 0;
            }
        }
    }

    /// <summary>
    /// Adds an item to the buffer. If buffer is full, oldest item is removed.
    /// </summary>
    /// <param name="item">Item to add</param>
    public void Add(T item)
    {
        lock (_syncLock)
        {
            _buffer[_tail] = item;
            _tail = (_tail + 1) % _capacity;

            if (_count == _capacity)
            {
                // Buffer is full, overwrite oldest
                _head = (_head + 1) % _capacity;
            }
            else
            {
                _count++;
            }
        }
    }

    /// <summary>
    /// Adds multiple items to the buffer
    /// </summary>
    /// <param name="items">Items to add</param>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (var item in items)
        {
            Add(item);
        }
    }

    /// <summary>
    /// Clears all items from the buffer
    /// </summary>
    public void Clear()
    {
        lock (_syncLock)
        {
            Array.Clear(_buffer, 0, _capacity);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }

    /// <summary>
    /// Removes and returns the oldest item from the buffer
    /// </summary>
    /// <param name="item">The dequeued item</param>
    /// <returns>True if an item was dequeued, false if buffer was empty</returns>
    public bool TryDequeue(out T? item)
    {
        lock (_syncLock)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }

            item = _buffer[_head];
            _buffer[_head] = default!;
            _head = (_head + 1) % _capacity;
            _count--;
            return true;
        }
    }

    /// <summary>
    /// Returns the oldest item without removing it
    /// </summary>
    /// <param name="item">The peeked item</param>
    /// <returns>True if an item was peeked, false if buffer was empty</returns>
    public bool TryPeek(out T? item)
    {
        lock (_syncLock)
        {
            if (_count == 0)
            {
                item = default;
                return false;
            }

            item = _buffer[_head];
            return true;
        }
    }

    /// <summary>
    /// Returns items from the buffer without removing them
    /// </summary>
    /// <param name="count">Number of items to return</param>
    /// <returns>Array of items (oldest first)</returns>
    public T[] Peek(int count)
    {
        lock (_syncLock)
        {
            if (count <= 0 || count > _count)
            {
                count = _count;
            }

            var items = new T[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = _buffer[(_head + i) % _capacity];
            }
            return items;
        }
    }

    /// <summary>
    /// Returns items from the start of the buffer without removing them (alias for Peek)
    /// </summary>
    /// <param name="count">Number of items to return</param>
    /// <returns>Array of items (oldest first)</returns>
    public T[] TryPeekFromStart(int count) => Peek(count);

    /// <summary>
    /// Converts the buffer contents to an array
    /// </summary>
    /// <returns>Array of all items in buffer (oldest to newest)</returns>
    public T[] ToArray()
    {
        lock (_syncLock)
        {
            var items = new T[_count];
            for (int i = 0; i < _count; i++)
            {
                items[i] = _buffer[(_head + i) % _capacity];
            }
            return items;
        }
    }

    /// <summary>
    /// Converts the buffer contents to a list
    /// </summary>
    /// <returns>List of all items in buffer (oldest to newest)</returns>
    public List<T> ToList()
    {
        lock (_syncLock)
        {
            var list = new List<T>(_count);
            for (int i = 0; i < _count; i++)
            {
                list.Add(_buffer[(_head + i) % _capacity]);
            }
            return list;
        }
    }

    /// <summary>
    /// Performs the specified action on each item
    /// </summary>
    /// <param name="action">Action to perform on each item</param>
    public void ForEach(Action<T> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        lock (_syncLock)
        {
            for (int i = 0; i < _count; i++)
            {
                action(_buffer[(_head + i) % _capacity]);
            }
        }
    }

    /// <summary>
    /// Extracts all items and clears the buffer
    /// </summary>
    /// <returns>List of all items that were in the buffer</returns>
    public List<T> ExtractAndClear()
    {
        lock (_syncLock)
        {
            var list = new List<T>(_count);
            for (int i = 0; i < _count; i++)
            {
                list.Add(_buffer[(_head + i) % _capacity]);
            }
            Clear();
            return list;
        }
    }

    /// <summary>
    /// Releases all resources used by the buffer
    /// </summary>
    public void Dispose()
    {
        Clear();
    }
}
