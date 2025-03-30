/********************************************************************************
* ClassProxySyntaxFactory.ConstructorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context)
        {
            foreach (IConstructorInfo ctor in TargetType.GetPublicConstructors())
            {
                cls = ResolveConstructor(cls, context, ctor);
            }
            return cls;
        }

        /// <summary>
        /// <code>
        /// public MyClass(IInterceptor interceptor, T param1, TT param2): base(param1, param2)
        /// {
        ///     FInterceptor = interceptor;
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructor(ClassDeclarationSyntax cls, object context, IConstructorInfo ctor)
        {
            string interceptor = EnsureUnused(nameof(interceptor), ctor);

            return cls.AddMembers
            (
                ResolveConstructor
                (
                    ctor,
                    cls.Identifier,
                    Parameter
                    (
                        identifier: Identifier(interceptor)
                    )
                    .WithType
                    (
                        type: ResolveType<IInterceptor>()
                    )
                )
                .WithBody
                (
                    Block
                    (
                        ExpressionStatement
                        (
                            AssignmentExpression
                            (
                                kind: SyntaxKind.SimpleAssignmentExpression,
                                left: ResolveIdentifierName(FInterceptor),
                                right: IdentifierName(interceptor)
                            )
                        )
                    )
                )
            );
        }
    }
}