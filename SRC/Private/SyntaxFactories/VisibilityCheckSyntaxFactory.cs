/********************************************************************************
* VisibilityCheckSyntaxFactory.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal sealed class VisibilityCheckSyntaxFactory : SyntaxFactoryBase, IUnitSyntaxFactory
    {
        public ITypeInfo Type { get; }

        public CompilationUnitSyntax? Unit { get; private set; }

        public OutputType OutputType => OutputType.Unit;

        public IReadOnlyCollection<string>? Classes => Array.Empty<string>();

        public VisibilityCheckSyntaxFactory(ITypeInfo type) => Type = type;

        public override bool Build(CancellationToken cancellation)
        {
            if (Unit is not null) return false;

            Unit = CompilationUnit().WithUsings
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

            return true;
        }
    }
}
