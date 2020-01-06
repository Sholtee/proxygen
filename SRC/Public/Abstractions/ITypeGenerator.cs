/********************************************************************************
* ITypeGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.Proxy.Abstractions
{
    /// <summary>
    /// Describes a type generator.
    /// </summary>
    public interface ITypeGenerator
    {
        /// <summary>
        /// The factory that provides the class definition.
        /// </summary>
        ISyntaxFactory SyntaxFactory { get; }

        /// <summary>
        /// Emits the <see cref="Type"/> compiled from the <see cref="SyntaxFactory"/> provided code.
        /// </summary>
        /// <remarks>The value of this property is generated only once.</remarks>
        Type GeneratedType { get; }

        /// <summary>
        /// References required to compile the code provided by the <see cref="SyntaxFactory"/>.
        /// </summary>
        IReadOnlyList<Assembly> References { get; }
    }
}
