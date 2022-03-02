/********************************************************************************
* ConcurrentHashSet.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class ConcurrentHashSet<T> : ICollection<T>
    {
        private readonly ConcurrentDictionary<T, byte> FUndelyingDictionary = new();

        public int Count => FUndelyingDictionary.Count;

        public bool IsReadOnly { get; }

        public void Add(T item) => FUndelyingDictionary.TryAdd(item, 0);

        public void Clear() => FUndelyingDictionary.Clear();

        public bool Contains(T item) => FUndelyingDictionary.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex) => FUndelyingDictionary.Keys.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => FUndelyingDictionary.Keys.GetEnumerator();

        public bool Remove(T item) => FUndelyingDictionary.TryRemove(item, out byte _);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
