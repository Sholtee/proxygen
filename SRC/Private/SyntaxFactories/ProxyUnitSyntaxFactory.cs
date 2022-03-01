/********************************************************************************
* ProxyUnitSyntaxFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal abstract class ProxyUnitSyntaxFactory : UnitSyntaxFactoryBase
    {
        protected ProxyUnitSyntaxFactory(OutputType outputType, string containingAssembly, ITypeInfo relatedGenerator, ReferenceCollector? referenceCollector): base(outputType, referenceCollector)
        {
            if (!relatedGenerator.GetBaseTypes().Some(@base => @base.QualifiedName == typeof(Generator).FullName))
                throw new ArgumentException(Resources.NOT_A_GENERATOR, nameof(relatedGenerator));

            RelatedGenerator = relatedGenerator;
            ContainingAssembly = containingAssembly;
        }

        public override ITypeInfo RelatedGenerator { get; }

        public override string ContainingAssembly { get; }

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
        protected override IEnumerable<MethodDeclarationSyntax> ResolveMethods(object context)
        {
            //
            // [ModuleInitializerAttribute]
            // public static void Initialize() => RegisterInstance(typeof(CurrentClass));
            //

            yield return MethodDeclaration
            (
                CreateType
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

                            CreateAttribute(typeof(InternalsVisibleToAttribute).Assembly.GetType("System.Runtime.CompilerServices.ModuleInitializerAttribute", throwOnError: false) ?? typeof(ModuleInitializerAttribute))
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
                                    IdentifierName
                                    (
                                        ResolveClassName(context)
                                    )
                                )
                            )
                        )
                    )
                )
            )
            .WithSemicolonToken
            (
                Token(SyntaxKind.SemicolonToken)
            );
        }
    }
}