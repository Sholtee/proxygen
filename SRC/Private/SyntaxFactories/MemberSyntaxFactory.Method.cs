/********************************************************************************
* MemberSyntaxFactory.Method.cs.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class MemberSyntaxFactory
    {
        /// <summary>
        /// [[(Type)] target | [(Type)] this | Namespace.Type].Method[...](...)
        /// </summary>
        protected internal MemberAccessExpressionSyntax MethodAccess(ExpressionSyntax? target, IMethodInfo method, ITypeInfo? castTargetTo = null) => SimpleMemberAccess
        (
            AmendTarget(target, method, castTargetTo),

            method is not IGenericMethodInfo genericMethod
                ? (SimpleNameSyntax) IdentifierName(method.Name)
                : (SimpleNameSyntax) GenericName(Identifier(method.Name)).WithTypeArgumentList
                (
                    typeArgumentList: TypeArgumentList
                    (
                        arguments: genericMethod.GenericArguments.ToSyntaxList(CreateType)
                    )
                )
        );

        /// <summary>
        /// int IInterface.Foo[T](string a, ref T b)
        /// </summary>
        protected internal virtual MethodDeclarationSyntax DeclareMethod(IMethodInfo method, bool forceInlining = false)
        {
            TypeSyntax returnTypeSytax = CreateType(method.ReturnValue.Type);

            if (method.ReturnValue.Kind >= ParameterKind.Ref)
            {
                RefTypeSyntax refReturnTypeSyntax = RefType(returnTypeSytax);

                if (method.ReturnValue.Kind == ParameterKind.RefReadonly)
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
                        ParameterSyntax parameter = Parameter(Identifier(param.Name)).WithType
                        (
                            type: CreateType(param.Type)
                        );

                        List<SyntaxKind> modifiers = new List<SyntaxKind>();

                        switch (param.Kind)
                        {
                            case ParameterKind.In:
                                modifiers.Add(SyntaxKind.InKeyword);
                                break;
                            case ParameterKind.Out:
                                modifiers.Add(SyntaxKind.OutKeyword);
                                break;
                            case ParameterKind.Ref:
                                modifiers.Add(SyntaxKind.RefKeyword);
                                break;
                            case ParameterKind.Params:
                                modifiers.Add(SyntaxKind.ParamsKeyword);
                                break;
                        }

                        if (modifiers.Any())
                            parameter = parameter.WithModifiers
                            (
                                TokenList(modifiers.Select(Token))
                            );

                        return parameter;
                    })
                )
            );

            if (method is IGenericMethodInfo genericMethod) result = result.WithTypeParameterList // kulon legyen kulonben lesz egy ures "<>"
            (
                typeParameterList: TypeParameterList
                (
                    parameters: genericMethod.GenericArguments.ToSyntaxList(type => TypeParameter(CreateType(type).ToFullString()))
                )
            );

            //
            // Interface implementaciokat mindig expliciten deklaraljuk
            //

            if (method.DeclaringType.IsInterface) result = result.WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(method.DeclaringType))
            );

            //
            // Kulonben a lathatosagnak meg kell egyeznie
            //

            else 
            {
                var modifiers = new List<SyntaxKind>();

                if (method.AccessModifiers.HasFlag(AccessModifiers.Public))
                    modifiers.Add(SyntaxKind.PublicKeyword);

                if (method.AccessModifiers.HasFlag(AccessModifiers.Protected))
                    modifiers.Add(SyntaxKind.ProtectedKeyword);

                if (method.AccessModifiers.HasFlag(AccessModifiers.Internal))
                    modifiers.Add(SyntaxKind.InternalKeyword);

                if (method.AccessModifiers.HasFlag(AccessModifiers.Private)) // private protected
                    modifiers.Add(SyntaxKind.PrivateKeyword);

                result = result.WithModifiers
                (
                    TokenList(modifiers.Select(Token))
                );
            }

            if (forceInlining) result = result.WithAttributeLists
            (
                attributeLists: DeclareMethodImplAttributeToForceInlining()
            );

            return result;
        }

        /// <summary>
        /// int IInterface.Foo[T](string a, ref T b)
        /// </summary>
        protected internal virtual MethodDeclarationSyntax OverrideMethod(IMethodInfo method, bool forceInlining = false)
        {
            MethodDeclarationSyntax result = DeclareMethod(method, forceInlining);
            
            return result.WithModifiers
            (
                TokenList
                (
                    result.Modifiers.Append // WithModifiers() felulcsapja a korabbi ertekeket
                    (
                        Token(SyntaxKind.OverrideKeyword)
                    )
                )
            );
        }

        /// <summary>
        /// target.Foo(..., ref ..., ...)
        /// </summary>
        protected internal virtual InvocationExpressionSyntax InvokeMethod(IMethodInfo method, ExpressionSyntax? target, ITypeInfo? castTargetTo = null, params ArgumentSyntax[] arguments)
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
                        (arg, i) => (paramz[i].Kind) switch
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
        /// target.Foo(ref a, b, c)
        /// </summary>
        protected internal virtual InvocationExpressionSyntax InvokeMethod(IMethodInfo method, ExpressionSyntax? target, ITypeInfo? castTargetTo = null, params string[] arguments)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

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
        protected internal virtual ConstructorDeclarationSyntax DeclareCtor(IConstructorInfo ctor, string className)
        {
            IReadOnlyList<IParameterInfo> paramz = ctor.Parameters;

            return ConstructorDeclaration
            (
                identifier: Identifier(className)
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
                        type: CreateType(param.Type)
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
