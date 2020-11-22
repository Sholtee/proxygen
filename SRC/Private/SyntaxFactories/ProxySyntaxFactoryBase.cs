﻿/********************************************************************************
* ProxySyntaxFactoryBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Abstractions;
    
    internal partial class ProxySyntaxFactoryBase: SyntaxFactoryBase, IProxySyntaxFactory
    {
        public virtual string ProxyClassName { get; } = "GeneratedProxy";

        protected static IEnumerable<MemberDeclarationSyntax> BuildMembers<TFactory>(IEnumerable<IMemberInfo> membersToBuild, CancellationToken cancellation) where TFactory : IInterceptorFactory, new()
        {
            cancellation.ThrowIfCancellationRequested();

            IInterceptorFactory fact = new TFactory();

            return membersToBuild
                .Where(fact.IsCompatible)
                .Select(fact.Build);
        }

        protected virtual MemberDeclarationSyntax GenerateProxyClass(CancellationToken cancellation) => throw new NotImplementedException();

        protected override CompilationUnitSyntax GenerateProxyUnit(CancellationToken cancellation)
        {
            return CompilationUnit().WithMembers
            (
                members: SingletonList<MemberDeclarationSyntax>
                (
                    GenerateProxyClass(cancellation)
                )
            )
            .WithAttributeLists
            (
                SingletonList
                (
                    AttributeList
                    (

                        new[] 
                        {
                            CreateAttribute<AssemblyTitleAttribute>(AsLiteral(AssemblyName!)),
                            CreateAttribute<AssemblyDescriptionAttribute>(AsLiteral("Generated by ProxyGen.NET"))
                        }
#if IGNORE_VISIBILITY
                        .Concat
                        (
                            ignoreAccessChecksTo.Select(asmName => (SyntaxNodeOrToken) CreateAttribute<IgnoresAccessChecksToAttribute>(AsLiteral(asmName)))
                        )
#endif
                        .ToSyntaxList()
                    )
                    .WithTarget
                    (
                        AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword))
                    )
                )
            );

            static LiteralExpressionSyntax AsLiteral(string param) => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(param));
        }
    }
}