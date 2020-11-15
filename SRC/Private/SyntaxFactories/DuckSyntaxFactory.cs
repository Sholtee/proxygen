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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory<TInterface, TTarget> : ProxySyntaxFactoryBase
    {
        private static readonly MemberAccessExpressionSyntax
            //
            // this.Target
            //
            TARGET = MemberAccess(null, MemberInfoExtensions.ExtractFrom<DuckBase<TTarget>>(ii => ii.Target!));

        public DuckSyntaxFactory() 
        {
            InterceptorFactories = new List<IInterceptorFactory>
            {
                new MethodInterceptorFactory(this),
                new PropertyInterceptorFactory(this),
                new EventInterceptorFactory(this)
            };
        }

        private IReadOnlyList<IInterceptorFactory> InterceptorFactories { get; }

        protected internal override ClassDeclarationSyntax GenerateProxyClass()
        {
            Type 
                interfaceType = typeof(TInterface),
                @base = typeof(DuckBase<TTarget>);

            Debug.Assert(interfaceType.IsInterface);
            Debug.Assert(!interfaceType.IsGenericTypeDefinition);
            Debug.Assert(!@base.IsGenericTypeDefinition);

            ClassDeclarationSyntax cls = ClassDeclaration
            (
                identifier: GeneratedClassName
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
                    new[] { @base, interfaceType }.ToSyntaxList(t => (BaseTypeSyntax) SimpleBaseType(CreateType(t)))
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
                    .Select(m => InterceptorFactories.SingleOrDefault(fact => fact.IsCompatible(m))?.Build(m))
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
