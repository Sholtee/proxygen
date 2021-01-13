/********************************************************************************
* CompilationExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal static class CompilationExtensions
    {
        public static IAssemblySymbol? GetAssemblyByLocation(this Compilation self, string location)
        {
            //
            // Ne uj MetadataReference peldanyt hozzunk letre, ugy nem fog mukpdni
            //

            MetadataReference? metadata = self.References.SingleOrDefault(@ref => @ref.Display == location);
            if (metadata is null)
                return null;

            return (IAssemblySymbol?) self.GetAssemblyOrModuleSymbol(metadata);
        }
    }
}
