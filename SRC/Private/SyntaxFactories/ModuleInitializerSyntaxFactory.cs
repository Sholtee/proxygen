﻿/********************************************************************************
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
    internal sealed class ModuleInitializerSyntaxFactory(SyntaxFactoryContext context) : UnitSyntaxFactoryBase(context)
    {
        public override string ExposedClass { get; } = nameof(ModuleInitializerAttribute);

        #if DEBUG
        internal
        #endif
        protected override IReadOnlyList<ITypeInfo> Bases { get; } = 
        [
            MetadataTypeInfo.CreateFrom(typeof(Attribute))
        ];

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
    }
}