/********************************************************************************
* ICodeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal interface ICodeFactory
    {
        bool ShouldUse(INamedTypeSymbol generator);
        IEnumerable<SourceCode> GetSourceCodes(INamedTypeSymbol generator, GeneratorExecutionContext context);

        //
        // Forrasgenerator nem bovitheti a mar meglevo referencia listat, szoval elvileg
        // a GetSourceCode() eleg is.
        //
    }
}
