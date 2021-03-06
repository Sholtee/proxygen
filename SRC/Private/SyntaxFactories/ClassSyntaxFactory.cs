﻿/********************************************************************************
* ClassSyntaxFactory.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal abstract class ClassSyntaxFactory : SyntaxFactoryBase, IUnitSyntaxFactory
    {
        #region Private
        private const string CONTAINING_NS = "Proxies";
        private IReadOnlyCollection<string>? FDefinedClasses;

        IReadOnlyCollection<string> IUnitSyntaxFactory.DefinedClasses => FDefinedClasses ??= new[]
        {
            OutputType switch
            {
                OutputType.Unit => CONTAINING_NS + Type.Delimiter + ClassName,
                OutputType.Module => ClassName,
                _ => throw new NotSupportedException()
            }
        };
        #endregion

        #region Protected
        protected abstract ClassDeclarationSyntax GenerateClass(IEnumerable<MemberDeclarationSyntax> members);

        protected virtual IEnumerable<MemberDeclarationSyntax> BuildMembers(CancellationToken cancellation)
        {
            foreach (IMemberSyntaxFactory syntaxFactory in MemberSyntaxFactories)
            {
                syntaxFactory.Build(cancellation);

                foreach (MemberDeclarationSyntax memberDeclaration in syntaxFactory.Members!)
                {
                    yield return memberDeclaration;
                }

                AddTypesFrom(syntaxFactory);
            }
        }

        protected ClassSyntaxFactory(OutputType outputType, string containingAssembly, ITypeInfo relatedGenerator)
        {
            OutputType = outputType;
            ContainingAssembly = containingAssembly;

            if (!relatedGenerator.GetBaseTypes().Any(@base => @base.QualifiedName == typeof(TypeGenerator<>).FullName))
                throw new ArgumentException(Resources.NOT_A_GENERATOR, nameof(relatedGenerator));

            RelatedGenerator = relatedGenerator;
        }
        #endregion

        #region Public
        public CompilationUnitSyntax? Unit { get; private set; }

        public OutputType OutputType { get; }

        public ITypeInfo RelatedGenerator { get; }

        public abstract IReadOnlyCollection<IMemberSyntaxFactory> MemberSyntaxFactories { get; }

        public abstract string ClassName { get; }

        public string ContainingAssembly { get; }

        public override bool Build(CancellationToken cancellation)
        {
            if (Unit is not null) return false;

            SyntaxList<MemberDeclarationSyntax> classImpl = SingletonList<MemberDeclarationSyntax>
            (
                GenerateClass
                (
                    BuildMembers(cancellation)
                )
                .WithAttributeLists
                (
                    SingletonList
                    (
                        Attributes
                        (
                            CreateAttribute<RelatedGeneratorAttribute>(TypeOf(RelatedGenerator)),
                            
                            //
                            // Kod-analizis figyelmeztetesek kikapcsolasa (plussz informativ):
                            // https://docs.microsoft.com/en-us/visualstudio/code-quality/in-source-suppression-overview?view=vs-2019#generated-code
                            //

                            CreateAttribute<GeneratedCodeAttribute>(AsLiteral("ProxyGen.NET"), AsLiteral(GetType().Assembly.GetName().Version.ToString())),

                            //
                            // Ezek pedig szerepelnek az xXx.Designer.cs-ben
                            //

                            CreateAttribute<DebuggerNonUserCodeAttribute>(),
                            CreateAttribute<CompilerGeneratedAttribute>()
                        )                
                    )
                )
            );

            Unit = OutputType switch
            {
                OutputType.Unit => CompilationUnit().WithMembers
                (
                    members: SingletonList<MemberDeclarationSyntax>
                    (
                        NamespaceDeclaration
                        (
                            IdentifierName(CONTAINING_NS)
                        )
                        .WithNamespaceKeyword
                        (
                            Token
                            (
                                TriviaList
                                (
                                    //
                                    // Az osszes fordito figyelmeztetes kikapcsolasa a generalt fajlban. Ha nincs azonosito lista megadva akkor
                                    // mindent kikapcsol:
                                    // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives#pragma-warning
                                    //

                                    Trivia(PragmaWarningDirectiveTrivia(Token(SyntaxKind.DisableKeyword), true))
                                ),
                                SyntaxKind.NamespaceKeyword,
                                TriviaList()
                            )
                        )
                        .WithMembers
                        (
                            members: classImpl
                        )
                    )
                ),
                OutputType.Module => CompilationUnit().WithMembers
                (
                    members: classImpl
                ),

                _ => throw new NotSupportedException()
            };

            return true;

            static LiteralExpressionSyntax AsLiteral(string param) => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(param));

            static AttributeListSyntax Attributes(params AttributeSyntax[] attributes) => AttributeList
            (
                attributes.ToSyntaxList()
            );
        }
        #endregion
    }
}