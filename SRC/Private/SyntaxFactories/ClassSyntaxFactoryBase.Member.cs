/********************************************************************************
* ClassSyntaxFactoryBase.Member.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

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

        /// <summary>
        /// Starting from .NET7.0 interfaces may have abstract static members. This method throw <see cref="NotSupportedException"/> in that case.
        /// </summary>
        private static void CheckNotStaticAbstract(IMemberInfo member)
        {
            if (member.IsAbstract && member.IsStatic)
                throw new NotSupportedException(Resources.ABSTRACT_STATIC_NOT_SUPPORTED);
        }

        protected static string EnsureUnused(ClassDeclarationSyntax cls, string member) => cls.Members.Any(m => m switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText == member,
            PropertyDeclarationSyntax prop => prop.Identifier.ValueText == member,
            EventDeclarationSyntax evt => evt.Identifier.ValueText == member,
            FieldDeclarationSyntax fld => ResolveIdentifierName(fld).Identifier.ValueText == member,
            _ => false
        }) ? EnsureUnused(cls, $"_{member}") : member;

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
        protected static MemberAccessExpressionSyntax StaticMemberAccess(ClassDeclarationSyntax target, string member) => StaticMemberAccess
        (
            target,
            IdentifierName(member)
        );

        #if DEBUG
        internal
        #endif
        protected static MemberAccessExpressionSyntax StaticMemberAccess(ClassDeclarationSyntax target, SimpleNameSyntax member) => SimpleMemberAccess
        (
            AliasQualifiedName
            (
                IdentifierName
                (
                    Token(SyntaxKind.GlobalKeyword)
                ),
                IdentifierName(target.Identifier)
            ),
            member
        );

        #if DEBUG
        internal
        #endif
        protected static MemberAccessExpressionSyntax StaticMemberAccess(ClassDeclarationSyntax target, MemberDeclarationSyntax member) => StaticMemberAccess
        (
            target,
            member switch 
            {
                FieldDeclarationSyntax fld => ResolveIdentifierName(fld),
                ClassDeclarationSyntax cls => ResolveIdentifierName(cls),
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
