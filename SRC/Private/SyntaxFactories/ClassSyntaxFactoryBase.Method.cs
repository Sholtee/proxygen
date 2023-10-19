/********************************************************************************
* ClassSyntaxFactoryBase.Method.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
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
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected MethodDeclarationSyntax ResolveMethod(IMethodInfo method, bool forceInlining = false)
        {
            TypeSyntax returnTypeSytax = ResolveType(method.ReturnValue.Type);

            if (method.ReturnValue.Kind >= ParameterKind.Ref)
            {
                RefTypeSyntax refReturnTypeSyntax = RefType(returnTypeSytax);

                if (method.ReturnValue.Kind is ParameterKind.RefReadonly)
                    refReturnTypeSyntax = refReturnTypeSyntax.WithReadOnlyKeyword
                    (
                        Token(SyntaxKind.ReadOnlyKeyword)
                    );

                returnTypeSytax = refReturnTypeSyntax;
            }

            MethodDeclarationSyntax result = MethodDeclaration
            (
                returnType: returnTypeSytax,
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
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) ResolveType(method.DeclaringType))
            );

            if (method is IGenericMethodInfo genericMethod)
            {
                result = result.WithTypeParameterList // kulon legyen kulonben lesz egy ures "<>"
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
                            genericMethod.GenericConstraints.ConvertAr
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
                                    GetContraints(constraint).ToSyntaxList()
                                ),
                                drop: constraint => !GetContraints(constraint).Some()
                            )
                        )
                    );

                    static IEnumerable<TypeParameterConstraintSyntax> GetContraints(IGenericConstraint constraint)
                    {
                        //
                        // Explicit interface implementations must not specify type constraits
                        //

                        if (constraint.Struct)
                            yield return ClassOrStructConstraint(SyntaxKind.StructConstraint);
                        if (constraint.Reference)
                            yield return ClassOrStructConstraint(SyntaxKind.ClassConstraint);
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
        protected InvocationExpressionSyntax InvokeMethod(IMethodInfo method, ExpressionSyntax? target, ITypeInfo? castTargetTo = null, params ArgumentSyntax[] arguments)
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
        protected InvocationExpressionSyntax InvokeMethod(IMethodInfo method, ExpressionSyntax? target, ITypeInfo? castTargetTo = null, params string[] arguments)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

            Debug.Assert(arguments.Length == paramz.Count);

            return InvokeMethod
            (
                method,
                target,
                castTargetTo,
                arguments: paramz.ConvertAr
                (
                    (param, i) => Argument
                    (
                        expression: IdentifierName(arguments[i])
                    )
                )
            );
        }
    
        #if DEBUG
        internal
        #endif
        protected abstract ClassDeclarationSyntax ResolveMethods(ClassDeclarationSyntax cls, object context);

        #if DEBUG
        internal
        #endif
        protected abstract ClassDeclarationSyntax ResolveMethod(ClassDeclarationSyntax cls, object context, IMethodInfo method);
    }
}
