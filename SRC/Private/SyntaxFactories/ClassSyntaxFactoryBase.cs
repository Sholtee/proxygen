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
        protected virtual ClassDeclarationSyntax ResolveClass(object context, CancellationToken cancellation)
        {
            ClassDeclarationSyntax cls = ClassDeclaration
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
                            ResolveType(t)
                        )
                    )
                )
            );

            return ResolveMembers(cls, context, cancellation);
        }

        protected virtual IEnumerable<Func<ClassDeclarationSyntax, object, ClassDeclarationSyntax>> MemberResolvers
        {
            get
            {
                yield return ResolveConstructors;
                yield return ResolveMethods;
                yield return ResolveProperties;
                yield return ResolveEvents;
            }
        }

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveMembers(ClassDeclarationSyntax cls, object context, CancellationToken cancellation)
        {
            foreach (Func<ClassDeclarationSyntax, object, ClassDeclarationSyntax> resolver in MemberResolvers)
            {
                cancellation.ThrowIfCancellationRequested();
                cls = resolver(cls, context);
            }
            return cls;
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