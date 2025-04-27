/********************************************************************************
* InternalFoo.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Tests.EmbeddedTypes
{
    internal abstract class InternalFoo<T>
    {
        public virtual T Prop { get; protected set; } = default!;
        public abstract T this[int i] { get; protected set; }
        public abstract event Action<T> Event;
        public virtual T Bar<TT>(ref T param1, TT param2) => param1;
    }
}
