/********************************************************************************
* TypeComparer.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Wrapper around the <see cref="TypeExtensions.EqualsTo(Type, Type)"/> method.
    /// </summary>
    internal sealed class TypeComparer : ComparerBase<TypeComparer, Type>
    {
        public override bool Equals(Type x, Type y) => x.EqualsTo(y);

        public override int GetHashCode(Type obj) => throw new NotImplementedException();
    }
}
