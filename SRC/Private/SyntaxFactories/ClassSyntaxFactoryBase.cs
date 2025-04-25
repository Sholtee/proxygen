/********************************************************************************
* ClassSyntaxFactoryBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ClassSyntaxFactoryBase(SyntaxFactoryContext context) : SyntaxFactoryBase(context)
    {
        #if DEBUG
        internal
        #endif
        protected abstract IReadOnlyList<ITypeInfo> Bases { get; }

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveClass(object context, CancellationToken cancellation) => ResolveMembers
        (
            ClassDeclaration
            (
                identifier: ExposedClass
            )
            .WithModifiers
            (
                modifiers: TokenList
                (
                    //
                    // Define our generated class as internal to let us use internal members, too
                    //

                    Token(SyntaxKind.InternalKeyword),
                    Token(SyntaxKind.SealedKeyword)
                )
            )
            .WithBaseList
            (
                baseList: BaseList
                (
                    Bases.ToSyntaxList
                    (
                        t => (BaseTypeSyntax) SimpleBaseType
                        (
                            ResolveType(t)
                        )
                    )
                )
            ),
            context,
            cancellation
        );

        #if DEBUG
        internal
        #endif
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
        protected virtual ClassDeclarationSyntax ResolveMembers(ClassDeclarationSyntax cls, object context, CancellationToken cancellation) => MemberResolvers.Aggregate
        (
            cls,
            (cls, resolver) =>
            {
                cancellation.ThrowIfCancellationRequested();
                return resolver(cls, context);
            }
        );
    }
}