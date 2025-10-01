using System;
using System.Collections;
using System.Collections.Generic;

namespace NinjaTrader.Custom.AddOns.OpenAutoATR
{

    /// <summary>
    /// High-performance circular buffer optimized for NinjaTrader 8 (.NET Framework 4.8)
    /// Provides O(1) operations and bounded memory usage for real-time trading
    /// </summary>
    public sealed class CircularBuffer<T> : IReadOnlyList<T>
    {
        private readonly T[] _buffer;
        private int _head;
        private int _count;

        public CircularBuffer(int capacity)
        {
            if (capacity <= 0) throw new ArgumentException("Capacity must be positive", nameof(capacity));
            _buffer = new T[capacity];
        }

        public int Count => _count;
        public int Capacity => _buffer.Length;
        public bool IsFull => _count == _buffer.Length;

        /// <summary>
        /// Add item with O(1) complexity. Overwrites oldest item when full.
        /// </summary>
        public void Add(T item)
        {
            _buffer[_head] = item;
            _head = (_head + 1) % _buffer.Length;

            if (_count < _buffer.Length)
                _count++;
        }

        /// <summary>
        /// Access by logical index (0 = oldest, Count-1 = newest)
        /// </summary>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException();

                int physicalIndex = (_head - _count + index + _buffer.Length) % _buffer.Length;
                return _buffer[physicalIndex];
            }
        }

        /// <summary>
        /// Get newest item (most recently added)
        /// </summary>
        public T Newest => _count > 0 ? this[_count - 1] : throw new InvalidOperationException("Buffer is empty");

        /// <summary>
        /// Get oldest item 
        /// </summary>
        public T Oldest => _count > 0 ? this[0] : throw new InvalidOperationException("Buffer is empty");

        /// <summary>
        /// Fill target array with last N items. Optimized for .NET Framework 4.8.
        /// </summary>
        public void CopyLastN(int n, T[] destination)
        {
            if (n <= 0) return;
            if (destination.Length < n) throw new ArgumentException("Destination too small");

            int actualN = Math.Min(n, _count);
            for (int i = 0; i < actualN; i++)
            {
                destination[i] = this[_count - actualN + i];
            }
        }

        /// <summary>
        /// Get array of the last N items - creates new array (use sparingly in hot paths)
        /// </summary>
        public T[] GetLastNArray(int n)
        {
            int actualN = Math.Min(n, _count);
            var result = new T[actualN];

            for (int i = 0; i < actualN; i++)
            {
                result[i] = this[_count - actualN + i];
            }

            return result;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
            // Optional: clear references for GC
            if (!typeof(T).IsValueType)
            {
                Array.Clear(_buffer, 0, _buffer.Length);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Extension methods for working with CircularBuffer in compute functions
    /// </summary>
    public static class CircularBufferExtensions
    {
        /// <summary>
        /// Extract values to existing List for compatibility with current compute functions
        /// </summary>
        public static void ExtractToList<T>(this CircularBuffer<T> buffer, int lookback, List<T> destination)
        {
            destination.Clear();

            int count = lookback > 0 ? Math.Min(lookback, buffer.Count) : buffer.Count;
            if (count == 0) return;

            if (destination.Capacity < count)
                destination.Capacity = count;

            int startIndex = buffer.Count - count;
            for (int i = 0; i < count; i++)
            {
                destination.Add(buffer[startIndex + i]);
            }
        }

        /// <summary>
        /// Get last N values as array (for compute functions that need arrays)
        /// Consider refactoring compute functions to use ReadOnlySpan<T> instead
        /// </summary>
        public static T[] GetLastN<T>(this CircularBuffer<T> buffer, int n)
        {
            int count = Math.Min(n, buffer.Count);
            var result = new T[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = buffer[buffer.Count - count + i];
            }

            return result;
        }

        /// <summary>
        /// Specialized method for double buffers - fills existing array
        /// </summary>
        public static void CopyLastNDoubles(this CircularBuffer<double> buffer, int n, double[] destination)
        {
            if (n <= 0) return;
            if (destination.Length < n) throw new ArgumentException("Destination too small");

            int actualN = Math.Min(n, buffer.Count);
            for (int i = 0; i < actualN; i++)
            {
                destination[i] = buffer[buffer.Count - actualN + i];
            }
        }

        /// <summary>
        /// Fast min/max calculation for double buffers
        /// </summary>
        public static (double min, double max) FindMinMax(this CircularBuffer<double> buffer)
        {
            if (buffer.Count == 0)
                return (0, 0);

            double min = buffer[0];
            double max = buffer[0];

            for (int i = 1; i < buffer.Count; i++)
            {
                double value = buffer[i];
                if (value < min) min = value;
                if (value > max) max = value;
            }

            return (min, max);
        }

        /// <summary>
        /// Fast min/max calculation for specific range in double buffer
        /// </summary>
        public static (double min, double max) FindMinMax(this CircularBuffer<double> buffer, int startIndex, int count)
        {
            if (buffer.Count == 0 || count <= 0)
                return (0, 0);

            int actualCount = Math.Min(count, buffer.Count - startIndex);
            if (actualCount <= 0)
                return (0, 0);

            double min = buffer[startIndex];
            double max = buffer[startIndex];

            for (int i = 1; i < actualCount; i++)
            {
                double value = buffer[startIndex + i];
                if (value < min) min = value;
                if (value > max) max = value;
            }

            return (min, max);
        }
    }
}
