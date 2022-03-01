/********************************************************************************
* ModuleInitializerSyntaxFactory.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal class ModuleInitializerSyntaxFactory : SyntaxFactoryBase
    {
        public ModuleInitializerSyntaxFactory() : base(null)
        {
        }

        public CompilationUnitSyntax ResolveUnit() => CompilationUnit().WithMembers
        (
            members: SingletonList<MemberDeclarationSyntax>
            (
                NamespaceDeclaration
                (
                    new[] { "System", "Runtime", "CompilerServices" }.Convert(IdentifierName).Qualify()
                )
                .WithNamespaceKeyword
                (
                    Token
                    (
                        TriviaList
                        (
                            Trivia
                            (
                                PragmaWarningDirectiveTrivia
                                (
                                    Token(SyntaxKind.DisableKeyword),
                                    true
                                )
                            )
                        ),
                        SyntaxKind.NamespaceKeyword,
                        TriviaList()
                    )
                )
                .WithMembers
                (
                    SingletonList<MemberDeclarationSyntax>
                    (
                        ClassDeclaration
                        (
                            nameof(ModuleInitializerAttribute)
                        )
                        .WithModifiers
                        (
                            TokenList(new[]{ Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword) })
                        )
                        .WithBaseList
                        (
                            BaseList
                            (
                                SingletonSeparatedList<BaseTypeSyntax>
                                (
                                    SimpleBaseType
                                    (
                                        CreateType<Attribute>()
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );
    }
}