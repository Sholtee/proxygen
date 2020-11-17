/********************************************************************************
* ProxySyntaxFactoryBase.Member.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactoryBase
    {
        private ExpressionSyntax AmendTarget(ExpressionSyntax? target, MemberInfo member, Type? castTargetTo)
        {
            target ??= member.IsStatic() ? CreateType(member.DeclaringType) : (ExpressionSyntax) ThisExpression();

            if (castTargetTo != null)
            {
                Debug.Assert(!member.IsStatic());

                target = ParenthesizedExpression
                (
                    CastExpression(CreateType(castTargetTo), target)
                );
            }

            return target;
        }

        protected internal static MemberAccessExpressionSyntax SimpleMemberAccess(ExpressionSyntax target, string member) => SimpleMemberAccess
        (
            target,
            IdentifierName(member)
        );

        protected internal static MemberAccessExpressionSyntax SimpleMemberAccess(ExpressionSyntax target, SimpleNameSyntax member) => MemberAccessExpression
        (
            SyntaxKind.SimpleMemberAccessExpression,
            target,
            member
        );

        /// <summary>
        /// [[(Type)] target | [(Type)] this | Namespace.Type].Member
        /// </summary>
        protected internal MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax? target, MemberInfo member, Type? castTargetTo = null) => SimpleMemberAccess
        (
            AmendTarget(target, member, castTargetTo),
            member.StrippedName()
        );
    }
}
