/********************************************************************************
* ModuleInitializerSyntaxFactory.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal class ModuleInitializerSyntaxFactory : UnitSyntaxFactoryBase
    {
        public ModuleInitializerSyntaxFactory(OutputType outputType, ReferenceCollector? referenceCollector) : base(outputType, referenceCollector)
        {
        }

        public override IReadOnlyCollection<string> DefinedClasses { get; } = new string[] { nameof(ModuleInitializerAttribute) };

        public override CompilationUnitSyntax ResolveUnit(object context, CancellationToken cancellation) => CompilationUnit().WithMembers
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
                        leading: TriviaList
                        (
                            Trivia
                            (
                                PragmaWarningDirectiveTrivia
                                (
                                    Token(SyntaxKind.DisableKeyword),
                                    true
                                )
                            ),
                            Trivia
                            (
                                IfDirectiveTrivia
                                (
                                    IdentifierName("NETSTANDARD"),
                                    isActive: true,
                                    branchTaken: false,
                                    conditionValue: false
                                )
                            )
                        ),
                        kind: SyntaxKind.NamespaceKeyword,
                        trailing: TriviaList()
                    )
                )
                .WithMembers
                (
                    List<MemberDeclarationSyntax>
                    (
                        ResolveClasses(context, cancellation)
                    )
                )
                .WithCloseBraceToken
                (
                    Token
                    (
                        leading: TriviaList(),
                        kind: SyntaxKind.CloseBraceToken,
                        trailing: TriviaList
                        (
                            Trivia
                            (
                                EndIfDirectiveTrivia(isActive: true)
                            )
                        )
                    )
                )
            )
        );

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ClassDeclarationSyntax> ResolveClasses(object context, CancellationToken cancellation)
        {
            yield return ResolveClass(context, cancellation);
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ITypeInfo> ResolveBases(object context)
        {
            yield return MetadataTypeInfo.CreateFrom(typeof(Attribute));
        }

        #if DEBUG
        internal
        #endif
        protected override string ResolveClassName(object context) => nameof(ModuleInitializerAttribute);

        #if DEBUG
        internal
        #endif
        protected override ConstructorDeclarationSyntax ResolveConstructor(object context, IConstructorInfo ctor) => throw new NotImplementedException();

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<ConstructorDeclarationSyntax> ResolveConstructors(object context)
        {
            yield break;
        }

        #if DEBUG
        internal
        #endif
        protected override EventDeclarationSyntax ResolveEvent(object context, IEventInfo evt) => throw new NotImplementedException();

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<EventDeclarationSyntax> ResolveEvents(object context)
        {
            yield break;
        }

        #if DEBUG
        internal
        #endif
        protected override MethodDeclarationSyntax ResolveMethod(object context, IMethodInfo method) => throw new NotImplementedException();

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MethodDeclarationSyntax> ResolveMethods(object context)
        {
            yield break;
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<BasePropertyDeclarationSyntax> ResolveProperties(object context)
        {
            yield break;
        }

        #if DEBUG
        internal
        #endif
        protected override BasePropertyDeclarationSyntax ResolveProperty(object context, IPropertyInfo property) => throw new NotImplementedException();
    }
}