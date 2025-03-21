/********************************************************************************
* ClassSyntaxFactoryBase.Method.cs                                              *
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

    internal partial class ClassSyntaxFactoryBase
    {
        /// <summary>
        /// <code>
        /// [[(Type)] target | [(Type)] this | Namespace.Type].Method&lt;...&gt;(...)
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected MemberAccessExpressionSyntax MethodAccess(ExpressionSyntax? target, IMethodInfo method, ITypeInfo? castTargetTo = null)
        {
            SimpleNameSyntax identifier = IdentifierName(method.Name);
            if (method is IGenericMethodInfo genericMethod)
                identifier = GenericName(identifier.Identifier).WithTypeArgumentList
                (
                    typeArgumentList: TypeArgumentList
                    (
                        arguments: genericMethod.GenericArguments.ToSyntaxList(ResolveType)
                    )
                );

            return SimpleMemberAccess
            (
                AmendTarget(target, method, castTargetTo),
                identifier
            );
        }

        /// <summary>
        /// <code>
        /// int IInterface.Foo&lt;...&gt;(T a, ref TT b) [where ...]
        /// </code>
        /// or
        /// <code>
        /// public override int Foo&lt;...&gt;(T a, ref TT b) [where ...]
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected MethodDeclarationSyntax ResolveMethod(IMethodInfo method, bool forceInlining = false)
        {
            TypeSyntax returnTypeSyntax = ResolveType(method.ReturnValue.Type);

            if (method.ReturnValue.Kind >= ParameterKind.Ref)
            {
                RefTypeSyntax refReturnTypeSyntax = RefType(returnTypeSyntax);

                if (method.ReturnValue.Kind is ParameterKind.RefReadonly)
                    refReturnTypeSyntax = refReturnTypeSyntax.WithReadOnlyKeyword
                    (
                        Token(SyntaxKind.ReadOnlyKeyword)
                    );

                returnTypeSyntax = refReturnTypeSyntax;
            }

            MethodDeclarationSyntax result = MethodDeclaration
            (
                returnType: returnTypeSyntax,
                identifier: Identifier(method.Name)
            )
            .WithParameterList
            (
                ParameterList
                (
                    parameters: method.Parameters.ToSyntaxList(param =>
                    {
                        ParameterSyntax parameter = Parameter
                        (
                            Identifier(param.Name)
                        )
                        .WithType
                        (
                            type: ResolveType(param.Type)
                        );

                        SyntaxKind? modifier = param.Kind switch
                        {
                            ParameterKind.In => SyntaxKind.InKeyword,
                            ParameterKind.Out => SyntaxKind.OutKeyword,
                            ParameterKind.Ref => SyntaxKind.RefKeyword,
                            ParameterKind.Params => SyntaxKind.ParamsKeyword,
                            _ => null
                        };

                        if (modifier is not null)
                            parameter = parameter.WithModifiers
                            (
                                TokenList(Token(modifier.Value))
                            );

                        return parameter;
                    })
                )
            );

            if (method.DeclaringType.IsInterface)
                result = result.WithExplicitInterfaceSpecifier
                (
                    explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) ResolveType(method.DeclaringType))
                );
            else
            {
                List<SyntaxKind> tokens = method
                    .AccessModifiers
                    .SetFlags()
                    .Select
                    (
                        am => am switch
                        {
                            AccessModifiers.Public => SyntaxKind.PublicKeyword,
                            AccessModifiers.Protected => SyntaxKind.ProtectedKeyword,
                            AccessModifiers.Internal => SyntaxKind.InternalKeyword,
                            _ => throw new ArgumentException(string.Format(Resources.METHOD_NOT_VISIBLE, method.Name), nameof(method)),
                        }
                    )
                    .ToList();

                tokens.Add(method.IsVirtual || method.IsAbstract ? SyntaxKind.OverrideKeyword : SyntaxKind.NewKeyword);

                result = result.WithModifiers
                (
                    TokenList
                    (
                        tokens.Select(Token)
                    )
                );
            }

            if (method is IGenericMethodInfo genericMethod)
            {
                result = result.WithTypeParameterList
                (
                    typeParameterList: TypeParameterList
                    (
                        parameters: genericMethod
                            .GenericArguments
                            .ToSyntaxList
                            (
                                type => TypeParameter
                                (
                                    ResolveType(type).ToFullString()
                                )
                            )
                    )
                );

                if (genericMethod.IsGenericDefinition)
                {
                    result = result.WithConstraintClauses
                    (
                        List
                        (
                            genericMethod
                                .GenericConstraints
                                .Where(constraint => GetConstraints(constraint).Any())
                                .Select
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
                    );

                    IEnumerable<TypeParameterConstraintSyntax> GetConstraints(IGenericConstraint constraint)
                    {
                        if (constraint.Struct)
                            yield return ClassOrStructConstraint(SyntaxKind.StructConstraint);
                        if (constraint.Reference)
                            yield return ClassOrStructConstraint(SyntaxKind.ClassConstraint);

                        //
                        // Explicit interface implementations must not specify type constraints
                        //

                        if (method.DeclaringType.IsInterface)
                            yield break;

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

            if (forceInlining) result = result.WithAttributeLists
            (
                attributeLists: ResolveMethodImplAttributeToForceInlining()
            );

            return result;
        }

        /// <summary>
        /// <code>
        /// target.Foo(..., ref ..., ...)
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected InvocationExpressionSyntax InvokeMethod(IMethodInfo method, ExpressionSyntax? target = null, ITypeInfo? castTargetTo = null, params ArgumentSyntax[] arguments)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

            Debug.Assert(arguments.Length == paramz.Count);

            return InvocationExpression
            (
                expression: MethodAccess
                (
                    target,
                    method,
                    castTargetTo
                )
            )
            .WithArgumentList
            (
                argumentList: ArgumentList
                (
                    arguments.ToSyntaxList
                    (
                        (arg, i) => paramz[i].Kind switch
                        {
                            ParameterKind.In => arg.WithRefKindKeyword
                            (
                                refKindKeyword: Token(SyntaxKind.InKeyword)
                            ),
                            ParameterKind.Out => arg.WithRefKindKeyword
                            (
                                refKindKeyword: Token(SyntaxKind.OutKeyword)
                            ),
                            ParameterKind.Ref => arg.WithRefKindKeyword
                            (
                                refKindKeyword: Token(SyntaxKind.RefKeyword)
                            ),
                            _ => arg
                        }
                    )
                )
            );
        }

        /// <summary>
        /// <code>
        /// target.Foo(ref a, b, c)
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected InvocationExpressionSyntax InvokeMethod(IMethodInfo method, ExpressionSyntax? target = null, ITypeInfo? castTargetTo = null, params string[] arguments)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

            Debug.Assert(arguments.Length == paramz.Count);

            return InvokeMethod
            (
                method,
                target,
                castTargetTo,
                arguments: paramz
                    .Select
                    (
                        (param, i) => Argument
                        (
                            expression: IdentifierName(arguments[i])
                        )
                    )
                    .ToArray()
            );
        }
    
        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveMethods(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveMethod(ClassDeclarationSyntax cls, object context, IMethodInfo method) => cls;
    }
}
