/********************************************************************************
* ModuleInitializerChunkFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

namespace Solti.Utils.Proxy.Internals
{
    internal static class ModuleInitializerChunkFactory
    {
        #pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [ModuleInitializer]
        #pragma warning restore CA2255
        public static void Init() => ProxyEmbedder.Chunks.Add
        (
            new ModuleInitializerSyntaxFactory(OutputType.Unit, null).GetSourceCode(default)
        );
    }
}
