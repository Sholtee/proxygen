﻿/********************************************************************************
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

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveUnitMembers(object context, CancellationToken cancellation)
        {
            yield return NamespaceDeclaration
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
        protected override IEnumerable<MemberDeclarationSyntax> ResolveConstructor(object context, IConstructorInfo ctor) => throw new NotImplementedException();

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveConstructors(object context)
        {
            yield break;
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveEvent(object context, IEventInfo evt) => throw new NotImplementedException();

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveEvents(object context)
        {
            yield break;
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveMethod(object context, IMethodInfo method) => throw new NotImplementedException();

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveMethods(object context)
        {
            yield break;
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveProperties(object context)
        {
            yield break;
        }

        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveProperty(object context, IPropertyInfo property) => throw new NotImplementedException();
    }
}