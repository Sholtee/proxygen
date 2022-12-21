/********************************************************************************
* ClassSyntaxFactoryBase.Member.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase
    {
        private ExpressionSyntax AmendTarget(ExpressionSyntax? target, IMemberInfo member, ITypeInfo? castTargetTo)
        {
            target ??= member.IsStatic ? ResolveType(member.DeclaringType) : (ExpressionSyntax) ThisExpression();

            if (castTargetTo is not null)
            {
                Debug.Assert(!member.IsStatic);

                target = ParenthesizedExpression
                (
                    CastExpression(ResolveType(castTargetTo), target)
                );
            }

            return target;
        }

        #if DEBUG
        internal
        #endif
        protected static MemberAccessExpressionSyntax SimpleMemberAccess(ExpressionSyntax target, string member) => SimpleMemberAccess
        (
            target,
            IdentifierName(member)
        );

        #if DEBUG
        internal
        #endif
        protected static MemberAccessExpressionSyntax SimpleMemberAccess(ExpressionSyntax target, SimpleNameSyntax member) => MemberAccessExpression
        (
            SyntaxKind.SimpleMemberAccessExpression,
            target,
            member
        );


        #if DEBUG
        internal
        #endif
        protected static MemberAccessExpressionSyntax StaticMemberAccess(ClassDeclarationSyntax target, MemberDeclarationSyntax member) => SimpleMemberAccess
        (
            AliasQualifiedName
            (
                IdentifierName
                (
                    Token(SyntaxKind.GlobalKeyword)
                ),
                IdentifierName(target.Identifier)
            ),
            member switch 
            {
                FieldDeclarationSyntax fld => ResolveIdentifierName(fld),
                ClassDeclarationSyntax cls => (SimpleNameSyntax) ResolveIdentifierName(cls),
                _ => throw new NotSupportedException() // TODO: method, prop, etc
            }
        );

        /// <summary>
        /// <code>
        /// [[(Type)] target | [(Type)] this | Namespace.Type].Member
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax? target, IMemberInfo member, ITypeInfo? castTargetTo = null) => SimpleMemberAccess
        (
            AmendTarget(target, member, castTargetTo),
            member.Name
        );
    }
}
