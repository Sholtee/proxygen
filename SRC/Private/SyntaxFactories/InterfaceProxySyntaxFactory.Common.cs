/********************************************************************************
* InterfaceProxySyntaxFactory.Common.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class InterfaceProxySyntaxFactory
    {
        #if DEBUG
        internal
        #else
        private
        #endif
        bool AlreadyImplemented<TMember>(TMember ifaceMember, IEnumerable<TMember> targetMembers, Func<TMember, TMember, bool> signatureEquals) where TMember : IMemberInfo
        {
            //
            // Starting from .NET7.0 interfaces may have abstract static members
            //

            if (ifaceMember.IsAbstract && ifaceMember.IsStatic)
                throw new NotSupportedException(Resources.ABSTRACT_STATIC_NOT_SUPPORTED);

            return
                InterceptorType.Interfaces.Any(iface => iface.EqualsTo(ifaceMember.DeclaringType)) ||
                targetMembers.Any(targetMember => signatureEquals(targetMember, ifaceMember));
        }

        /// <summary>
        /// <code>
        /// InterfaceMap&lt;TInterface, TTarget&gt;.Value | null
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        ExpressionSyntax ResolveInterfaceMap()
        {
            if (TargetType.IsInterface)
                return LiteralExpression(SyntaxKind.NullLiteralExpression);

            IGenericTypeInfo map = 
            (
                (IGenericTypeInfo) MetadataTypeInfo.CreateFrom(typeof(InterfaceMap<,>))
            ).Close(InterfaceType, TargetType);

            return MemberAccessExpression
            (
                SyntaxKind.SimpleMemberAccessExpression,
                ResolveType(map),
                IdentifierName(nameof(InterfaceMap<object, object>.Value))
            );
        }

        /// <summary>
        /// <code>
        /// private static readonly InterfaceInterceptionContext FXxX = new InterfaceInterceptionContext(static (object target, object[] args) => 
        /// {                                                                                               
        ///     System.Int32 cb_a = (System.Int32) args[0];                                                
        ///     System.String cb_b;                                                                     
        ///     TT cb_c = (TT) args[2];                                                                    
        ///     ...                                                                                    
        /// }, CALL_INDEX, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        FieldDeclarationSyntax ResolveMethodContext(string id, ParenthesizedLambdaExpressionSyntax lambda, int callIndex) => ResolveField<InterfaceInterceptionContext>
        (
            $"F{id}",
            ResolveObject<InterfaceInterceptionContext>
            (
                Argument(lambda),
                Argument
                (
                    callIndex.AsLiteral()
                ),
                Argument
                (
                    ResolveInterfaceMap()
                )
            )     
        );

        /// <summary>
        /// <code>
        /// private static class WrapperXxX&lt;T1, T2, ...&gt; [where ...]                                            
        /// {                                                                                                   
        ///     public static readonly InterfaceInterceptionContext Value = new InterfaceInterceptionContext(static (object target, object[] args) => 
        ///     {                                                                                               
        ///         System.Int32 cb_a = (System.Int32) args[0];                                                  
        ///         System.String cb_b;                                                                      
        ///         T1 cb_c = (T1) args[2];                                                                 
        ///         ...                                                                                     
        ///     }, CALL_INDEX, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        ClassDeclarationSyntax ResolveMethodContext(string id, ParenthesizedLambdaExpressionSyntax lambda, int callIndex, IEnumerable<ITypeInfo> genericArguments, IEnumerable<IGenericConstraint> constraints)
        {
            return ClassDeclaration
            (
                $"Wrapper{id}"
            )
            .WithModifiers
            (
                TokenList
                (
                    new SyntaxToken[]
                    {
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.StaticKeyword)
                    }
                )
            )
            .WithTypeParameterList
            (
                TypeParameterList
                (
                    genericArguments.ToSyntaxList(ga =>
                    {
                        Debug.Assert(ga.IsGenericParameter, "Argument must be a generic parameter");
                        return TypeParameter(ga.Name);
                    })
                )
            )
            .WithConstraintClauses
            (
                List
                (
                    constraints.Select
                    (
                        constraint => TypeParameterConstraintClause
                        (
                            IdentifierName
                            (
                                constraint.Target.Name  // T, T, etc
                            )
                        )
                        .WithConstraints
                        (
                            GetConstraints(constraint).ToSyntaxList()
                        )
                    )
                )
            )
            .AddMembers
            (
                ResolveField<InterfaceInterceptionContext>
                (
                    $"Value",
                    ResolveObject<InterfaceInterceptionContext>
                    (
                        Argument(lambda),
                        Argument
                        (
                            callIndex.AsLiteral()
                        ),
                        Argument
                        (
                            ResolveInterfaceMap()
                        )
                    ),
                    @private: false
                )
            );

            IEnumerable<TypeParameterConstraintSyntax> GetConstraints(IGenericConstraint constraint)
            {
                if (constraint.Struct)
                    yield return ClassOrStructConstraint(SyntaxKind.StructConstraint);
                if (constraint.Reference)
                    yield return ClassOrStructConstraint(SyntaxKind.ClassConstraint);
                if (constraint.DefaultConstructor)
                    yield return ConstructorConstraint();

                foreach (ITypeInfo typeConstraint in constraint.ConstraintTypes)
                {
                    yield return TypeConstraint
                    (
                        ResolveType(typeConstraint)
                    );
                }
            }
        }
    }
}