/********************************************************************************
* UnitSyntaxFactoryBase.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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
    internal abstract class UnitSyntaxFactoryBase : ClassSyntaxFactoryBase
    {
        private static AttributeListSyntax Attributes(params AttributeSyntax[] attributes) => AttributeList
        (
            attributes.ToSyntaxList()
        );

        protected UnitSyntaxFactoryBase(OutputType outputType, ReferenceCollector? referenceCollector, LanguageVersion languageVersion): base(referenceCollector, languageVersion) =>
            OutputType = outputType;

        #if DEBUG
        internal
        #endif
        protected abstract IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation);

        public OutputType OutputType { get; }

        public abstract string ExposedClass { get; }

        public virtual CompilationUnitSyntax ResolveUnit(object context, CancellationToken cancellation)
        {
            List<MemberDeclarationSyntax> members = new
            (
                ResolveUnitMembers(context, cancellation)
            );

            if (members.Any() && OutputType is OutputType.Unit)
            {
                members[0] = members[0] switch
                {
                    NamespaceDeclarationSyntax ns => ns.WithNamespaceKeyword
                    (
                        Token
                        (
                            leading: DisableWarnings(ns.NamespaceKeyword),
                            kind: SyntaxKind.NamespaceKeyword,
                            trailing: TriviaList()
                        )
                    ),
                    ClassDeclarationSyntax cls => cls.WithAttributeLists
                    (
                        cls.AttributeLists.Replace
                        (
                            //
                            // Due to ResolveUnitMembers(), we always have attributes
                            //

                            cls.AttributeLists[0],
                            cls.AttributeLists[0].WithOpenBracketToken
                            (
                                Token
                                (
                                    leading: DisableWarnings(cls.AttributeLists[0].OpenBracketToken),
                                    kind: SyntaxKind.OpenBracketToken,
                                    trailing: TriviaList()
                                )
                            )
                        )
                    ),
                    _ => members[0]
                };

                static SyntaxTriviaList DisableWarnings(SyntaxToken token) =>
                    //
                    // Disable all compiler warnings:
                    // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives#pragma-warning
                    //

                    token.LeadingTrivia.Insert
                    (
                        0,
                        Trivia
                        (
                            PragmaWarningDirectiveTrivia
                            (
                                Token(SyntaxKind.DisableKeyword),
                                isActive: true
                            )
                        )
                    );
            }

            return CompilationUnit().WithMembers
            (
                List(members)
            );
        }

        #if DEBUG
        internal
        #endif
        protected virtual IEnumerable<MemberDeclarationSyntax> ResolveUnitMembers(object context, CancellationToken cancellation) => ResolveClasses(context, cancellation).Select
        (
            cls => cls.WithAttributeLists
            (
                SingletonList
                (
                    Attributes
                    (
                        //
                        // https://docs.microsoft.com/en-us/visualstudio/code-quality/in-source-suppression-overview?view=vs-2019#generated-code
                        //

                        ResolveAttribute<GeneratedCodeAttribute>
                        (
                            "ProxyGen.NET".AsLiteral(),
                            GetType()
                                .Assembly
                                .GetName()
                                .Version
                                .ToString()
                                .AsLiteral()
                        ),

                        //
                        // See xXx.Designer.cs
                        //

                        ResolveAttribute<DebuggerNonUserCodeAttribute>(),
                        ResolveAttribute<CompilerGeneratedAttribute>()
                    )
                )
            )
        );
    }
}