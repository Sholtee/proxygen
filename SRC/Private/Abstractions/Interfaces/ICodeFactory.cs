/********************************************************************************
* ICodeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ICodeFactory
    {
        string GeneratorFullName { get; }
        IEnumerable<SourceCode> GetSourceCodes(INamedTypeSymbol generator, GeneratorExecutionContext context);

        //
        // Forrasgenerator nem bovitheti a mar meglevo referencia listat, szoval elvileg
        // a GetSourceCode() eleg is.
        //
    }
}
