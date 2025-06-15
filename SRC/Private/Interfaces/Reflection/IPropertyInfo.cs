/********************************************************************************
* IPropertyInfo.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Specifies the abstraction of property metadata we want to inspect.  
    /// </summary>
    internal interface IPropertyInfo: IMemberInfo, IHasType
    {
        /// <summary>
        /// The "get" accessor (if declared).
        /// </summary>
        IMethodInfo? GetMethod { get; }

        /// <summary>
        /// The "set" accessor (if declared).
        /// </summary>
        IMethodInfo? SetMethod { get; }

        /// <summary>
        /// The indices (if any) or an empty list.
        /// </summary>
        /// <remarks>
        /// <code>
        /// // Indexer declaration
        /// public int this[int index]
        /// {
        ///    // get and set accessors
        /// }
        /// </code>
        /// </remarks>
        IReadOnlyList<IParameterInfo> Indices { get; }
    }
}
