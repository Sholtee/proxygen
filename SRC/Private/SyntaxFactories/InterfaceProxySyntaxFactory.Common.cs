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
        static string EnsureUnused(string name, IEnumerable<IParameterInfo> parameters) 
        {
            while (parameters.Any(param => param.Name == name))
            {
                name = $"_{name}";
            }
            return name;
        }

        #if DEBUG
        internal
        #else
        private
        #endif
        static string EnsureUnused(string name, IMethodInfo method) => EnsureUnused(name, method.Parameters);

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
        /// TResult IInterface.Foo&lt;TGeneric&gt;(T1 para1, ref T2 para2, out T3 para3, TGeneric para4) 
        /// {                                                                                
        ///   ...                                                                          
        ///   object[] args = new object[]{para1, para2, default(T3), para4};              
        ///   ...                                                                            
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        LocalDeclarationStatementSyntax ResolveArgumentsArray(IMethodInfo method)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

            return ResolveLocal<object[]>
            (
                EnsureUnused("args", paramz),
                ResolveArray<object>
                (
                    paramz.Select
                    (
                        param => (ExpressionSyntax) 
                        (
                            param.Kind switch
                            {
                                _ when param.Type.RefType is RefType.Ref =>
                                    //
                                    // We cannot cast "ref struct"s to objects
                                    //

                                    throw new NotSupportedException(Resources.BYREF_NOT_SUPPORTED),
                                ParameterKind.Out => DefaultExpression
                                (
                                    ResolveType(param.Type)
                                ),
                                _ => IdentifierName(param.Name)
                            }
                        )
                    )
                )
            );
        }

        /// <summary>
        /// <code>
        /// return;
        /// // OR
        /// return (T) ...;
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, ExpressionSyntax result) => ReturnStatement
        (
            expression: returnType?.IsVoid is true
                ? null
                : returnType is not null
                    ? CastExpression
                    (
                        type: ResolveType(returnType),
                        expression: result
                    )
                    : result
        );

        /// <summary>
        /// <code>
        /// return;
        /// // OR
        /// return (T) result;
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, LocalDeclarationStatementSyntax result) =>
            ReturnResult(returnType, ResolveIdentifierName(result));

        /// <summary>
        /// <code>
        /// return null;
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        static ReturnStatementSyntax ReturnNull() => ReturnStatement
        (
            LiteralExpression(SyntaxKind.NullLiteralExpression)
        );

        /// <summary>
        /// <code>
        /// System.String _a;
        /// TT _b = (TT) args[1];
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        LocalDeclarationStatementSyntax[] ResolveInvokeTargetLocals(ParameterSyntax argsArray, IMethodInfo method) => method
            .Parameters
            .Select
            (
                (p, i) => ResolveLocal
                (
                    p.Type,
                    $"_{p.Name}", // will be utilized in static method -> EnsureUnused() not necessary
                    p.Kind is ParameterKind.Out ? null : CastExpression
                    (
                        type: ResolveType(p.Type),
                        expression: ElementAccessExpression
                        (
                            IdentifierName(argsArray.Identifier)
                        )
                        .WithArgumentList
                        (
                            argumentList: BracketedArgumentList
                            (
                                SingletonSeparatedList
                                (
                                    Argument
                                    (
                                        i.AsLiteral()
                                    )
                                )
                            )
                        )
                    )
                )
            )
            .ToArray();

        /// <summary>
        /// <code>
        /// static (object target, object[] args) =>   
        /// {                                             
        ///     System.Int32 cb_a = (System.Int32) args[0]; 
        ///     System.String cb_b;                   
        ///     TT cb_c = (TT) args[2];                
        ///     ...                                          
        /// };
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #else
        private
        #endif
        ParenthesizedLambdaExpressionSyntax ResolveInvokeTarget(IMethodInfo method, Action<ParameterSyntax, ParameterSyntax, IReadOnlyList<LocalDeclarationStatementSyntax>, List<StatementSyntax>> invocationFactory)
        {
            ParameterSyntax
                target = Parameter
                (
                    identifier: Identifier(nameof(target))
                )
                .WithType
                (
                    ResolveType<object>()
                ),
                args = Parameter
                (
                    identifier: Identifier(nameof(args))
                )
                .WithType
                (
                    ResolveType<object[]>()
                );

            List<StatementSyntax> statements = new();

            IReadOnlyList<LocalDeclarationStatementSyntax> locals = ResolveInvokeTargetLocals(args, method);
            statements.AddRange(locals);

            invocationFactory(target, args, locals, statements);

            ParenthesizedLambdaExpressionSyntax lambda = ParenthesizedLambdaExpression()
                .WithParameterList
                (
                    ParameterList
                    (
                        new ParameterSyntax[] { target, args }.ToSyntaxList()
                    )
                )
                .WithBody
                (
                    Block(statements)
                );

            if (LanguageVersion >= LanguageVersion.CSharp9)
                lambda = lambda.WithModifiers
                (
                    modifiers: TokenList
                    (
                        Token(SyntaxKind.StaticKeyword)
                    )
                );
            return lambda;
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
        /// private static readonly MethodContext FXxX = new MethodContext(static (object target, object[] args) => 
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
        FieldDeclarationSyntax ResolveMethodContext(ParenthesizedLambdaExpressionSyntax lambda, int callIndex) => ResolveStaticGlobal<InterfaceInterceptionContext>
        (
            $"F{lambda.GetMD5HashCode()}",
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
        ///     public static readonly MethodContext Value = new MethodContext(static (object target, object[] args) => 
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
        ClassDeclarationSyntax ResolveMethodContext(ParenthesizedLambdaExpressionSyntax lambda, int callIndex, IEnumerable<ITypeInfo> genericArguments, IEnumerable<IGenericConstraint> constraints)
        {
            return ClassDeclaration
            (
                $"Wrapper{lambda.GetMD5HashCode()}"
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
                ResolveStaticGlobal<InterfaceInterceptionContext>
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