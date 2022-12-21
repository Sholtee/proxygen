/********************************************************************************
* ProxyUnitSyntaxFactory.Initializer.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveInitializer(ClassDeclarationSyntax cls, object context) => cls.AddMembers
        (
            //
            // [ModuleInitializerAttribute]
            // public static void Initialize() => LoadedTypes.Register(typeof(CurrentClass)); // C# 7.0 compatible
            //

            MethodDeclaration
            (
                ResolveType
                (
                    MetadataTypeInfo.CreateFrom(typeof(void))
                ),
                Identifier("Initialize")
            )
            .WithModifiers
            (
                TokenList
                (
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword)
                )
            )
            .WithAttributeLists
            (
                SingletonList
                (
                    AttributeList
                    (
                        SingletonSeparatedList
                        (
                            ResolveAttribute(typeof(ModuleInitializerAttribute))
                        )
                    )
                )
            )
            .WithExpressionBody
            (
                ArrowExpressionClause
                (
                    InvokeMethod
                    (
                        MetadataMethodInfo.CreateFrom
                        (
                            (MethodInfo) MemberInfoExtensions.ExtractFrom(() => LoadedTypes.Register(null!))
                        ),
                        target: null,
                        castTargetTo: null,
                        Argument
                        (
                            TypeOfExpression
                            (
                                AliasQualifiedName
                                (
                                    IdentifierName
                                    (
                                        Token(SyntaxKind.GlobalKeyword)
                                    ),
                                    IdentifierName(cls.Identifier)
                                )
                            )
                        )
                    )
                )
            )
            .WithSemicolonToken
            (
                Token(SyntaxKind.SemicolonToken)
            )
        );
    }
}