/********************************************************************************
* ISyntaxFactory.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Abstractions
{
    /// <summary>
    /// Describes an abstract syntax factory.
    /// </summary>
    public interface ISyntaxFactory
    {
        /// <summary>
        /// If the syntax should be compiled, this property returns the name of containing assembly.
        /// </summary>
        string? AssemblyName { get; }

        /// <summary>
        /// Returns the compilation unit (namespace, class defintion, etc) of the defined class.
        /// </summary>
        (CompilationUnitSyntax Unit, IReadOnlyCollection<MetadataReference> References, IReadOnlyCollection<Type> Types) GetContext(CancellationToken cancellation = default);
    }
}
