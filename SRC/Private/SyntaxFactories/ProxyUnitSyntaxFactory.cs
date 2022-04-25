/********************************************************************************
* ProxyUnitSyntaxFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract class ProxyUnitSyntaxFactory : UnitSyntaxFactoryBase
    {
        protected ProxyUnitSyntaxFactory(OutputType outputType, string containingAssembly, ReferenceCollector? referenceCollector): base(outputType, referenceCollector) =>
            ContainingAssembly = containingAssembly;

        public string ContainingAssembly { get; }

        //
        // Proxy egyseg mindig csak egy osztalyt definial
        //

        public override IReadOnlyCollection<string> DefinedClasses => new string[]
        {
            ResolveClassName(null!)
        };

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMethods(ClassDeclarationSyntax cls, object context) => cls.AddMembers
        (
            //
            // [ModuleInitializerAttribute]
            // public static void Initialize() => RegisterInstance(typeof(CurrentClass));
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
                            //
                            // Ne legyen nev utkozes
                            //

                            ResolveAttribute(typeof(InternalsVisibleToAttribute).Assembly.GetType("System.Runtime.CompilerServices.ModuleInitializerAttribute", throwOnError: false) ?? typeof(ModuleInitializerAttribute))
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
                            (MethodInfo) MemberInfoExtensions.ExtractFrom(() => GeneratedClass.RegisterInstance(null!))
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