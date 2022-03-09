/********************************************************************************
* ModuleInitializerChunkFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class ModuleInitializerChunkFactory: IChunkFactory
    {
        #pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [ModuleInitializer]
        #pragma warning restore CA2255
        public static void Init() => IChunkFactory.Registered.Entries.Add(new ModuleInitializerChunkFactory());

        public bool ShouldUse(IRuntimeContext context, string? assembly)
        {
            //
            // Azert a bonyolult ellenorzes mert a ModuleInitializerAttribute-t mi magunk is
            // definialhatjuk
            //

            ITypeInfo? type = context.GetTypeByQualifiedName(typeof(ModuleInitializerAttribute).FullName);
            if (type is not null)
            {
                if (type.AccessModifiers is AccessModifiers.Public)
                    return false;

                if (type.AccessModifiers is AccessModifiers.Internal && assembly is not null && type.DeclaringAssembly!.IsFriend(assembly))
                    return false;
            }
            return true;
        }

        public SourceCode GetSourceCode(in CancellationToken cancellation) => new ModuleInitializerSyntaxFactory(OutputType.Unit, null).GetSourceCode(cancellation);
    }
}
