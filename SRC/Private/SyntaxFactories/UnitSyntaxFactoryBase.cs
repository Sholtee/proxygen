/********************************************************************************
* UnitSyntaxFactoryBase.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract class UnitSyntaxFactoryBase : ClassSyntaxFactoryBase, IUnitDefinition
    {
        protected UnitSyntaxFactoryBase(OutputType outputType, ReferenceCollector? referenceCollector): base(referenceCollector) =>
            OutputType = outputType;

        #if DEBUG
        internal
        #endif
        protected abstract IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation);

        public OutputType OutputType { get; }

        public abstract IReadOnlyCollection<string> DefinedClasses { get; }

        public virtual CompilationUnitSyntax ResolveUnit(object context, CancellationToken cancellation)
        {
            return CompilationUnit().WithMembers
            (
                List<MemberDeclarationSyntax>
                (
                    ResolveClasses(context, cancellation).Convert
                    (
                        cls => cls.WithAttributeLists
                        (
                            SingletonList
                            (
                                Attributes
                                (
                                    //
                                    // Kod-analizis figyelmeztetesek kikapcsolasa (plussz informativ):
                                    // https://docs.microsoft.com/en-us/visualstudio/code-quality/in-source-suppression-overview?view=vs-2019#generated-code
                                    //

                                    CreateAttribute<GeneratedCodeAttribute>
                                    (
                                        AsLiteral("ProxyGen.NET"),
                                        AsLiteral
                                        (
                                            GetType()
                                                .Assembly
                                                .GetName()
                                                .Version
                                                .ToString()
                                        )
                                    ),

                                    //
                                    // Ezek pedig szerepelnek az xXx.Designer.cs-ben
                                    //

                                    CreateAttribute<DebuggerNonUserCodeAttribute>(),
                                    CreateAttribute<CompilerGeneratedAttribute>()
                                )
                                .WithOpenBracketToken
                                (
                                    Token
                                    (
                                        OutputType is not OutputType.Unit ? TriviaList() : TriviaList
                                        (
                                            Trivia
                                            (
                                                //
                                                // Az osszes fordito figyelmeztetes kikapcsolasa a generalt fajlban. Ha nincs azonosito lista megadva akkor
                                                // mindent kikapcsol:
                                                // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives#pragma-warning
                                                //

                                                PragmaWarningDirectiveTrivia
                                                (
                                                    Token(SyntaxKind.DisableKeyword),
                                                    true
                                                )
                                            )
                                        ),
                                        SyntaxKind.OpenBracketToken,
                                        TriviaList()
                                    )
                                )
                            )
                        )
                    )
                )
            );

            static LiteralExpressionSyntax AsLiteral(string param) => LiteralExpression
            (
                SyntaxKind.StringLiteralExpression,
                Literal(param)
            );

            static AttributeListSyntax Attributes(params AttributeSyntax[] attributes) => AttributeList
            (
                attributes.ToSyntaxList()
            );
        }
    }
}