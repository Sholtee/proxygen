/********************************************************************************
* SymbolAssemblyInfo.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

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

        public static IAssemblyInfo CreateFrom(IAssemblySymbol underlyingSymbol, Compilation compilation) => new SymbolAssemblyInfo(underlyingSymbol, compilation);

        public string? Location => Compilation
            .References
            .First(reference => SymbolEqualityComparer.Default.Equals(UnderlyingSymbol, Compilation.GetAssemblyOrModuleSymbol(reference)))
            .Display;

        public bool IsDynamic => false; // forditas idoben nem lehet dinamikus ASM hivatkozva

        public string Name => UnderlyingSymbol.Identity.ToString();

        public bool IsFriend(string asmName) => UnderlyingSymbol.Name == asmName || UnderlyingSymbol.GivesAccessTo // TODO: strong name support
        (
            CSharpCompilation.Create(asmName).Assembly
        );

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(UnderlyingSymbol);

        public override bool Equals(object obj) => obj is SymbolAssemblyInfo that && SymbolEqualityComparer.Default.Equals(that.UnderlyingSymbol, UnderlyingSymbol);

        public override string ToString() => UnderlyingSymbol.ToString();
    }
}
