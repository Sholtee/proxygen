/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory<TInterface, TTarget> : ProxySyntaxFactoryBase
    {
        //
        // this.Target
        //

        private readonly MemberAccessExpressionSyntax TARGET;

        public DuckSyntaxFactory() 
        {
            InterceptorFactories = new List<IInterceptorFactory>
            {
                new MethodInterceptorFactory(this),
                new PropertyInterceptorFactory(this),
                new EventInterceptorFactory(this)
            };

            TARGET = MemberAccess(null, MemberInfoExtensions.ExtractFrom<DuckBase<TTarget>>(ii => ii.Target!));
        }

        private IReadOnlyList<IInterceptorFactory> InterceptorFactories { get; }

        protected override MemberDeclarationSyntax GenerateProxyClass(CancellationToken cancellation)
        {
            Type 
                interfaceType = typeof(TInterface),
                @base = typeof(DuckBase<TTarget>);

            Debug.Assert(interfaceType.IsInterface);
            Debug.Assert(!interfaceType.IsGenericTypeDefinition);
            Debug.Assert(!@base.IsGenericTypeDefinition);

            ClassDeclarationSyntax cls = ClassDeclaration
            (
                identifier: ProxyClassName
            )
            .WithModifiers
            (
                modifiers: TokenList
                (
                    //
                    // Az osztaly ne publikus legyen h "internal" lathatosagu tipusokat is hasznalhassunk
                    //

                    Token(SyntaxKind.InternalKeyword),
                    Token(SyntaxKind.SealedKeyword)
                )
            )
            .WithBaseList
            (
                baseList: BaseList
                (
                    new[] { @base, interfaceType }.ToSyntaxList(t => (BaseTypeSyntax) SimpleBaseType
                    (
                        CreateType(t)
                    ))
                )
            );

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>
            (
                @base.GetPublicConstructors().Select(DeclareCtor)
            );

            members.AddRange
            (
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                interfaceType
                    .ListMembers<MemberInfo>()
                    .Select(m => 
                    {
                        cancellation.ThrowIfCancellationRequested();
                        return InterceptorFactories.SingleOrDefault(fact => fact.IsCompatible(m))?.Build(m);
                    })
                    .Where(m => m != null)
#pragma warning restore CS8620
            );

            return cls.WithMembers
            (
                List(members)
            );
        }

        public override string AssemblyName => $"{GetSafeTypeName<TTarget>()}_{GetSafeTypeName<TInterface>()}_Duck";
    }
}
