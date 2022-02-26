/********************************************************************************
* SymbolAssemblyInfo.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    internal class SymbolAssemblyInfo : IAssemblyInfo
    {
        private IAssemblySymbol UnderlyingSymbol { get; }

        private Compilation Compilation { get; }

        private SymbolAssemblyInfo(IAssemblySymbol underlyingSymbol, Compilation compilation) 
        {
            UnderlyingSymbol = underlyingSymbol;
            Compilation = compilation;
        }

        public static IAssemblyInfo CreateFrom(IAssemblySymbol asm, Compilation compilation)
        {
            asm.EnsureNotError();

            return new SymbolAssemblyInfo(asm, compilation);
        }

        public string? Location
        {
            get
            {
                if (SymbolEqualityComparer.Default.Equals(UnderlyingSymbol, Compilation.Assembly))
                    //
                    // Az epp forditas alatt levo szerelvenynek meg tuti nincs eleresi utvonala
                    //

                    return null;

                foreach (MetadataReference reference in Compilation.References)
                {
                    if (SymbolEqualityComparer.Default.Equals(UnderlyingSymbol, Compilation.GetAssemblyOrModuleSymbol(reference)))
                    {
                        //
                        // Ha a Compilation GeneratorExecutionContext-bol jon es nem teljes ujraforditas
                        // van akkor a MetadataReference.Display nem biztos h eleresi utvonal
                        //

                        return !string.IsNullOrEmpty(reference.Display) && File.Exists(reference.Display)
                            ? reference.Display
                            : null;
                    }
                }

                return null;
            }
        }

        public bool IsDynamic => false; // forditas idoben nem lehet dinamikus ASM hivatkozva

        public string Name => UnderlyingSymbol.Identity.ToString();

        public bool IsFriend(string asmName) => StringComparer.OrdinalIgnoreCase.Equals(UnderlyingSymbol.Name, asmName) || UnderlyingSymbol.GivesAccessTo // TODO: strong name support
        (
            CSharpCompilation.Create(asmName).Assembly
        );

        public ITypeInfo? GetType(string fullName)
        {
            INamedTypeSymbol? type = UnderlyingSymbol.GetTypeByMetadataName(fullName);

            return type is not null
                ? SymbolTypeInfo.CreateFrom(type, Compilation)
                : null;
        }

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override bool Equals(object obj) => obj is SymbolAssemblyInfo that && SymbolEqualityComparer.Default.Equals(that.UnderlyingSymbol, UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();
    }
}
