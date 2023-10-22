/********************************************************************************
* ModuleInitializerSyntaxFactory.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal class ModuleInitializerSyntaxFactory : UnitSyntaxFactoryBase
    {
        public ModuleInitializerSyntaxFactory(OutputType outputType, ReferenceCollector? referenceCollector = null, LanguageVersion languageVersion = LanguageVersion.Latest) : base(outputType, referenceCollector, languageVersion)
        {
        }

        public override IReadOnlyCollection<string> DefinedClasses { get; } = new string[] { nameof(ModuleInitializerAttribute) };

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveUnitMembers(object context, CancellationToken cancellation)
        {
            yield return NamespaceDeclaration
            (
                new[] { "System", "Runtime", "CompilerServices" }.Select(IdentifierName).Qualify()
            )
            .WithNamespaceKeyword
            (
                Token
                (
                    leading: TriviaList
                    (
                        Trivia
                        (
                            IfDirectiveTrivia
                            (
                                PrefixUnaryExpression
                                (
                                    SyntaxKind.LogicalNotExpression,
                                    IdentifierName("NET5_0_OR_GREATER")
                                ),
                                isActive: true,
                                branchTaken: true,
                                conditionValue: true
                            )
                        )
                    ),
                    kind: SyntaxKind.NamespaceKeyword,
                    trailing: TriviaList()
                )
            )
            .WithMembers
            (
                List
                (
                    base.ResolveUnitMembers(context, cancellation)
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
            );
        }

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
        protected override ClassDeclarationSyntax ResolveConstructor(ClassDeclarationSyntax cls, object context, IConstructorInfo ctor) => cls;

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo evt) => cls;

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvents(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMethod(ClassDeclarationSyntax cls, object context, IMethodInfo method) => cls;

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveMethods(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperty(ClassDeclarationSyntax cls, object context, IPropertyInfo property) => cls;
    }
}