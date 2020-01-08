/********************************************************************************
* ITypeGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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
        /// References required to compile the code provided by the <see cref="SyntaxFactory"/>.
        /// </summary>
        IReadOnlyList<Assembly> References { get; }
    }
}
