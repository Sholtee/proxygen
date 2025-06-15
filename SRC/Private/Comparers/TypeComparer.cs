/********************************************************************************
* TypeComparer.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Wrapper around the <see cref="TypeExtensions.EqualsTo(Type, Type)"/> method. This comparer is required to properly handle open generic arguments when comparing generic <see cref="Type"/>s
    /// </summary>
    internal sealed class TypeComparer : ComparerBase<TypeComparer, Type>
    {
        /// <inheritdoc/>
        public override bool Equals(Type x, Type y) => x.EqualsTo(y);

        /// <summary>
        /// This method is NOT implemented and will throw <see cref="NotImplementedException"/>
        /// </summary>
        public override int GetHashCode(Type obj) => throw new NotImplementedException();
    }
}
