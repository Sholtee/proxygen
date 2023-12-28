/********************************************************************************
* ProxyUnitSyntaxFactory.Initializer.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactory
    {
        /// <summary>
        /// <code>
        /// [ModuleInitializerAttribute]
        /// public static void Initialize() => LoadedTypes.Register(typeof(CurrentClass)); // C# 7.0 compatible
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveInitializer(ClassDeclarationSyntax cls, object context) => cls.AddMembers
        (
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
                            MethodInfoExtensions.ExtractFrom<object>(_ => LoadedTypes.Register(null!))
                        ),
                        arguments: Argument
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