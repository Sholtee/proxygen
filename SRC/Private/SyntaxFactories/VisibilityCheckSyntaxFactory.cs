/********************************************************************************
* VisibilityCheckSyntaxFactory.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class VisibilityCheckSyntaxFactory : SyntaxFactoryBase
    {
        public ITypeInfo Type { get; }

        public VisibilityCheckSyntaxFactory(ITypeInfo type) => Type = type;

        protected override CompilationUnitSyntax GenerateProxyUnit(CancellationToken cancellation) => CompilationUnit().WithUsings
        (
            usings: SingletonList
            (
                UsingDirective
                (
                    name: (NameSyntax) CreateType(Type)
                )
                .WithAlias
                (
                    alias: NameEquals(IdentifierName("t"))
                )
            )
        );
    }
}
