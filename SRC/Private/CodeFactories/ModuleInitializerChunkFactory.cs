/********************************************************************************
* ModuleInitializerChunkFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class ModuleInitializerChunkFactory: IChunkFactory
    {
        #pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [ModuleInitializer]
        #pragma warning restore CA2255
        public static void Init() => ProxyEmbedder.ChunkFactories.Add(new ModuleInitializerChunkFactory());

        public bool ShouldUse(Compilation compilation)
        {
            //
            // Azert a bonyolult ellenorzes mert a ModuleInitializerAttribute-t mi magunk is
            // definialhatjuk
            //

            INamedTypeSymbol? type = compilation.GetTypeByMetadataName(typeof(ModuleInitializerAttribute).FullName);
            if (type is null)
                return true;

            ITypeInfo typeInfo = SymbolTypeInfo.CreateFrom(type, compilation);
            if (type.DeclaredAccessibility is Accessibility.Public)
                return false;

            return typeInfo.DeclaringAssembly?.IsFriend(compilation.Assembly.Name) is not true;
        }

        public SourceCode GetSourceCode(in CancellationToken cancellation) => new ModuleInitializerSyntaxFactory(OutputType.Unit, null).GetSourceCode(cancellation);
    }
}
