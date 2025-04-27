/********************************************************************************
* IReferenceCollector.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Defines the contract how to collect references.
    /// </summary>
    internal interface IReferenceCollector
    {
        /// <summary>
        /// Registers a new type (and its assembly). In case of generic types this method should call itself to generic arguments recursively. If the type is already known this method is a noop
        /// </summary>
        void AddType(ITypeInfo type);

        /// <summary>
        /// Types encountered by this collector. Each list entry should be unique, in other words this is a distinct list.
        /// </summary>
        IReadOnlyCollection<ITypeInfo> Types { get; }

        /// <summary>
        /// Assemblies encountered by this collector. Each list entry should be unique, in other words this is a distinct list.
        /// </summary>
        IReadOnlyCollection<IAssemblyInfo> References { get; }
    }
}
