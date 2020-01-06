/********************************************************************************
* ISyntaxFactory.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Abstractions
{
    /// <summary>
    /// Describes a syntax factory that is responsible for defining a class.
    /// </summary>
    /// <remarks>Each factory should define only one class.</remarks>
    public interface ISyntaxFactory
    {
        /// <summary>
        /// The name of the assembly that contains the defined class.
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        /// The name of the defined class.
        /// </summary>
        string GeneratedClassName { get; }

        /// <summary>
        /// Returns the compilation unit (namespace, class defintion, etc) of the defined class.
        /// </summary>
        CompilationUnitSyntax GenerateProxyUnit();
    }
}
