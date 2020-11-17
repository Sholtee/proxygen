/********************************************************************************
* ProxySyntaxFactoryBase.Method.cs.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactoryBase
    {
        /// <summary>
        /// [[(Type)] target | [(Type)] this | Namespace.Type].Method[...](...)
        /// </summary>
        protected internal MemberAccessExpressionSyntax MethodAccess(ExpressionSyntax? target, MethodInfo method, Type? castTargetTo = null)
        {
            string methodName = method.StrippedName();

            return SimpleMemberAccess
            (
                AmendTarget(target, method, castTargetTo),
                !method.IsGenericMethod
                    ? (SimpleNameSyntax) IdentifierName(methodName)
                    : (SimpleNameSyntax) GenericName(Identifier(methodName)).WithTypeArgumentList
                    (
                        typeArgumentList: TypeArgumentList
                        (
                            arguments: method.GetGenericArguments().ToSyntaxList(CreateType)
                        )
                    )
            );
        }

        /// <summary>
        /// int IInterface.Foo[T](string a, ref T b)
        /// </summary>
        protected internal virtual MethodDeclarationSyntax DeclareMethod(MethodInfo method, bool forceInlining = false)
        {
            Type
                declaringType = method.DeclaringType,
                returnType = method.ReturnType;

            Debug.Assert(declaringType.IsInterface);

            TypeSyntax returnTypeSytax = CreateType(returnType);

            if (returnType.IsByRef)
                returnTypeSytax = RefType(returnTypeSytax);

            MethodDeclarationSyntax result = MethodDeclaration
            (
                returnType: returnTypeSytax,
                identifier: Identifier(method.StrippedName())
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(declaringType))
            )
            .WithParameterList
            (
                ParameterList
                (
                    parameters: method.GetParameters().ToSyntaxList(param =>
                    {
                        ParameterSyntax parameter = Parameter(Identifier(param.Name)).WithType
                        (
                            type: CreateType(param.ParameterType)
                        );

                        List<SyntaxKind> modifiers = new List<SyntaxKind>();

                        switch (param.GetParameterKind())
                        {
                            case ParameterKind.In:
                                modifiers.Add(SyntaxKind.InKeyword);
                                break;
                            case ParameterKind.Out:
                                modifiers.Add(SyntaxKind.OutKeyword);
                                break;
                            case ParameterKind.InOut:
                                modifiers.Add(SyntaxKind.RefKeyword);
                                break;
                            case ParameterKind.Params:
                                modifiers.Add(SyntaxKind.ParamsKeyword);
                                break;
                        }

                        if (modifiers.Any())
                            parameter = parameter.WithModifiers(TokenList(modifiers.Select(Token)));

                        return parameter;
                    })
                )
            );

            if (method.IsGenericMethod) result = result.WithTypeParameterList // kulon legyen kulonben lesz egy ures "<>"
            (
                typeParameterList: TypeParameterList
                (
                    parameters: method.GetGenericArguments().ToSyntaxList(type => TypeParameter(CreateType(type).ToFullString()))
                )
            );

            if (forceInlining) result = result.WithAttributeLists
            (
                attributeLists: DeclareMethodImplAttributeToForceInlining()
            );

            //
            // Interface metodus nem lehet "async" ezert nem kell ellenorizni h rendelkezik
            // e "AsyncStateMachineAttribute" attributummal.
            //

            return result;
        }

        /// <summary>
        /// target.Foo(..., ref ..., ...)
        /// </summary>
        protected internal virtual InvocationExpressionSyntax InvokeMethod(MethodInfo method, ExpressionSyntax? target, Type? castTargetTo = null, params ArgumentSyntax[] arguments)
        {
            IReadOnlyList<ParameterInfo> paramz = method.GetParameters();

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
                        (arg, i) => (paramz[i].GetParameterKind()) switch
                        {
                            ParameterKind.In => arg.WithRefKindKeyword
                            (
                                refKindKeyword: Token(SyntaxKind.InKeyword)
                            ),
                            ParameterKind.Out => arg.WithRefKindKeyword
                            (
                                refKindKeyword: Token(SyntaxKind.OutKeyword)
                            ),
                            ParameterKind.InOut => arg.WithRefKindKeyword
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
        /// target.Foo(ref a, b, c)
        /// </summary>
        protected internal virtual InvocationExpressionSyntax InvokeMethod(MethodInfo method, ExpressionSyntax? target, Type? castTargetTo = null, params string[] arguments)
        {
            IReadOnlyList<ParameterInfo> paramz = method.GetParameters();

            Debug.Assert(arguments.Length == paramz.Count);

            return InvokeMethod
            (
                method,
                target,
                castTargetTo,
                arguments: paramz
                    .Select((param, i) => Argument
                    (
                        expression: IdentifierName(arguments[i])
                    ))
                    .ToArray()
            );
        }

        /// <summary>
        /// TypeName(int a, string b, ...): base(a, b, ...){ }
        /// </summary>
        protected internal virtual ConstructorDeclarationSyntax DeclareCtor(ConstructorInfo ctor)
        {
            IReadOnlyList<ParameterInfo> paramz = ctor.GetParameters();

            return ConstructorDeclaration
            (
                identifier: Identifier(ProxyClassName)
            )
            .WithModifiers
            (
                modifiers: TokenList
                (
                    Token(SyntaxKind.PublicKeyword)
                )
            )
            .WithParameterList
            (
                parameterList: ParameterList(paramz.ToSyntaxList
                (
                    param => Parameter
                    (
                        identifier: Identifier(param.Name)
                    )
                    .WithType
                    (
                        type: CreateType(param.ParameterType)
                    ))
                )
            )
            .WithInitializer
            (
                initializer: ConstructorInitializer
                (
                    SyntaxKind.BaseConstructorInitializer,
                    ArgumentList(paramz.ToSyntaxList(param => Argument
                    (
                        expression: IdentifierName(param.Name)
                    )))
                )
            )
            .WithBody(Block());
        }
    }
}
