/********************************************************************************
* ClassSyntaxFactoryBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ClassSyntaxFactoryBase: SyntaxFactoryBase
    {
        public ClassSyntaxFactoryBase(ReferenceCollector? referenceCollector) : base(referenceCollector) { }

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveClass(object context, CancellationToken cancellation) => 
            ClassDeclaration
            (
                identifier: ResolveClassName(context)
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
                    ResolveBases(context).ToSyntaxList
                    (
                        t => (BaseTypeSyntax) SimpleBaseType
                        (
                            CreateType(t)
                        )
                    )
                )
            )
            .WithMembers
            (
                List
                (
                    ResolveMembers(context, cancellation)
                )
            );

        #if DEBUG
        internal
        #endif
        protected virtual IEnumerable<MemberDeclarationSyntax> ResolveMembers(object context, CancellationToken cancellation)
        {
            foreach (Func<object, IEnumerable<MemberDeclarationSyntax>> factory in new Func<object, IEnumerable<MemberDeclarationSyntax>>[] { ResolveConstructors, ResolveMethods, ResolveProperties, ResolveEvents })
            {
                foreach (MemberDeclarationSyntax member in factory(context))
                {
                    cancellation.ThrowIfCancellationRequested();
                    yield return member;
                }
            }
        }

        #if DEBUG
        internal
        #endif
        protected abstract string ResolveClassName(object context);

        #if DEBUG
        internal
        #endif
        protected abstract IEnumerable<ITypeInfo> ResolveBases(object context);
    }
}