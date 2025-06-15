/********************************************************************************
* IGenericConstraint.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Describes an abstract generic constraint.
    /// </summary>
    internal interface IGenericConstraint
    {
        /// <summary>
        /// The type must have a default constructor: <code>where T: new()</code>
        /// </summary>
        bool DefaultConstructor { get; }

        /// <summary>
        /// The type must be a reference type: <code>where T: class</code>
        /// </summary>
        bool Reference { get; }

        /// <summary>
        /// The type must be a value type: <code>where T: struct</code>
        /// </summary>
        bool Struct { get; }

        /// <summary>
        /// The underlying generic argument.
        /// </summary>
        ITypeInfo Target { get; }

        /// <summary>
        /// The generic types constraints (for instance interfaces that must be implemented): <code>where T: IMyInterface</code>
        /// </summary>
        IReadOnlyList<ITypeInfo> ConstraintTypes { get; }
    }
}
