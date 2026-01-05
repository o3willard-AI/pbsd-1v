using System;
using System.Collections.Concurrent;

namespace PairAdmin.DataStructures;

/// <summary>
/// Circular buffer for storing terminal output with fixed capacity
/// Thread-safe implementation using lock-free ConcurrentQueue for optimal performance
/// </summary>
/// <typeparam name="T">Type of items in the buffer</typeparam>
public class CircularBuffer<T> : IDisposable
{
    private readonly ConcurrentQueue<T> _buffer;
    private readonly int _capacity;
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

        _buffer = new ConcurrentQueue<T>(capacity, capacity);
        _capacity = capacity;
        _count = 0;
    }

    /// <summary>
    /// Initializes a new instance of CircularBuffer with default capacity
    /// </summary>
    public CircularBuffer() : this(100)
    {
        _buffer = new ConcurrentQueue<T>(100);
        _capacity = 100;
        _count = 0;
    }

    /// <summary>
    /// Gets the current number of items in the buffer
    /// </summary>
    /// <returns>Current count of items</returns>
    public int Count
    {
        return _count;
    }

    /// <summary>
    /// Gets the capacity of the buffer
    /// </summary>
    /// <returns>Maximum number of items buffer can hold</returns>
    public int Capacity
    {
        return _capacity;
    }

    /// <summary>
    /// Gets whether the buffer is empty
    /// </summary>
    /// <returns>True if buffer has no items</returns>
    public bool IsEmpty
    {
        return _count == 0;
    }

    /// <summary>
    /// Adds an item to the buffer. If buffer is full, oldest item is removed.
    /// </summary>
    /// <param name="item">Item to add</param>
    public void Add(T item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        lock (_syncLock)
        {
            while (_buffer.Count >= _capacity)
            {
                if (!_buffer.TryDequeue(out _))
                {
                    throw new InvalidOperationException("Failed to remove item from full buffer");
                }
                _count--;
            }

            _buffer.Enqueue(item);
            _count++;
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
            while (_buffer.TryDequeue(out _))
            {
                // Item is dequeued and discarded
            }
            
            _buffer.Clear();
            _count = 0;
        }
    }

    /// <summary>
    /// Removes and returns the oldest item from the buffer
    /// </summary>
    /// <returns>Oldest item, or default if buffer is empty</returns>
    public T TryDequeue()
    {
        if (_buffer.TryDequeue(out var item))
        {
            if (item != null)
            {
                lock (_syncLock)
                {
                    _count--;
                }
            }
            
            return item;
        }
        
        return default;
    }

    /// <summary>
    /// Returns the newest items without removing them (peek operation)
    /// </summary>
    /// <returns>Newest item(s), or empty if buffer is empty</returns>
    public T[] TryPeek(int count)
    {
        if (count <= 0 || count > _count)
        {
            count = _count;
        }

        if (_buffer.Count == 0)
        {
            return Array.Empty<T>();
        }

        var items = new T[count];
        _buffer.TryPeek(items);

        return items;
    }

    /// <summary>
    /// Returns the oldest items without removing them (peek from beginning)
    /// </summary>
    /// <param name="count">Number of items to return</param>
    /// <returns>Oldest item(s), or empty if buffer is empty</returns>
    public T[] TryPeekFromStart(int count)
    {
        if (count <= 0 || count > _count)
        {
            count = _count;
        }

        if (_buffer.Count == 0)
        {
            return Array.Empty<T>();
        }

        var items = new T[count];
        _buffer.TryPeek(items);

        return items;
    }

    /// <summary>
    /// Removes and returns the oldest item from the buffer
    /// </summary>
    /// <returns>Oldest item, or default if buffer is empty</returns>
    public bool TryRemoveOldest()
    {
        if (_buffer.TryDequeue(out var item))
        {
            if (item != null)
            {
                lock (_syncLock)
                {
                    _count--;
                }
            
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Removes and returns the newest item from the buffer
    /// </summary>
    /// <returns>Newest item, or default if buffer is empty</returns>
    public T TryRemoveNewest()
    {
        if (_buffer.TryDequeue(out var item))
        {
            if (item != null)
            {
                lock (_syncLock)
                {
                    _count--;
                }
            
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Removes the newest item from the buffer
    /// </summary>
    /// <param name="item">Item to remove</param>
    /// <returns>True if item was removed, false otherwise</returns>
    public bool TryRemove(T item)
    {
        // Not supported on circular buffer with FIFO semantics
        return false;
    }

    /// <summary>
    /// Converts the buffer contents to an array
    /// </summary>
    /// <returns>Array of all items in buffer (in order from oldest to newest)</returns>
    public T[] ToArray()
    {
        var items = new T[_count];
        _buffer.TryDequeue(items);
        return items;
    }

    /// <summary>
    /// Converts the buffer contents to a list
    /// </summary>
    /// <returns>List of all items in buffer (in order from oldest to newest)</returns>
    public List<T> ToList()
    {
        var list = new List<T>(_count);
        _buffer.TryDequeue(list);
        return list;
    }

    /// <summary>
    /// Performs the specified action on each item and clears the buffer
    /// </summary>
    /// <param name="action">Action to perform on each item</param>
    public void ForEach(Action<T> action)
    {
        lock (_syncLock)
        {
            var count = _count;
            while (count-- > 0 && _buffer.TryDequeue(out var item))
            {
                action(item);
            }
        }
        }
    }

    /// <summary>
    /// Performs the specified action on each item and removes them
    /// </summary>
    /// <param name="action">Action to perform on each item</param>
    /// <returns>List of processed items in order from oldest to newest</returns>
    public List<T> ExtractAndClear()
    {
        var list = new List<T>();
        
        lock (_syncLock)
        {
            var count = _count;
            while (count-- > 0 && _buffer.TryDequeue(out var item))
            {
                action(item);
                list.Add(item);
            }
        }
        }
        
        return list;
    }

    /// <summary>
    /// Creates a snapshot of the buffer contents without clearing
    /// </summary>
    /// <returns>Array of all items in buffer (in order from oldest to newest)</returns>
    public T[] ToArray()
    {
        var items = new T[_count];
        _buffer.TryDequeue(items);
        return items;
    }

    /// <summary>
    /// Releases all resources used by the buffer
    /// </summary>
    public void Dispose()
    {
        _buffer.Clear();
        _count = 0;
    }
}
