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
    internal sealed class SymbolAssemblyInfo(IAssemblySymbol underlyingSymbol, Compilation compilation) : IAssemblyInfo
    {
        private IAssemblySymbol UnderlyingSymbol { get; } = underlyingSymbol;

        public static IAssemblyInfo CreateFrom(IAssemblySymbol asm, Compilation compilation)
        {
            asm.EnsureNotError();

            return new SymbolAssemblyInfo(asm, compilation);
        }

        private readonly Lazy<string?> FLocation = new(() =>
        {
            if (SymbolEqualityComparer.Default.Equals(underlyingSymbol, compilation.Assembly))
                //
                // Assembly being compiled doesn't have path
                //

                return null;

            foreach (MetadataReference reference in compilation.References)
            {
                if (SymbolEqualityComparer.Default.Equals(underlyingSymbol, compilation.GetAssemblyOrModuleSymbol(reference)))
                {
                    //
                    // MetadataReference.Display is not necessarily a path
                    //

                    return !string.IsNullOrEmpty(reference.Display) && File.Exists(reference.Display)
                        ? reference.Display
                        : null;
                }
            }

            return null;
        });
        public string? Location => FLocation.Value;

        public bool IsDynamic => false; // We cannot reference dynamic ASMs during build

        public string Name => UnderlyingSymbol.Identity.ToString();

        public bool IsFriend(string asmName) => StringComparer.OrdinalIgnoreCase.Equals(UnderlyingSymbol.Name, asmName) || UnderlyingSymbol.GivesAccessTo // TODO: strong name support
        (
            CSharpCompilation.Create(asmName).Assembly
        );

        public ITypeInfo? GetType(string fullName)
        {
            INamedTypeSymbol? type = UnderlyingSymbol.GetTypeByMetadataName(fullName);

            return type is not null
                ? SymbolTypeInfo.CreateFrom(type, compilation)
                : null;
        }

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override bool Equals(object obj) => obj is SymbolAssemblyInfo that && SymbolEqualityComparer.Default.Equals(that.UnderlyingSymbol, UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();
    }
}
