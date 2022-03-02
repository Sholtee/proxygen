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
        /// [[(Type)] target | [(Type)] this | Namespace.Type].Method[...](...)
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
                        arguments: genericMethod.GenericArguments.ToSyntaxList(CreateType)
                    )
                );

            return SimpleMemberAccess(AmendTarget(target, method, castTargetTo), identifier);
        }

        /// <summary>
        /// int IInterface.Foo[T](string a, ref T b)
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected MethodDeclarationSyntax DeclareMethod(IMethodInfo method, bool forceInlining = false)
        {
            TypeSyntax returnTypeSytax = CreateType(method.ReturnValue.Type);

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
                        ParameterSyntax parameter = Parameter(Identifier(param.Name)).WithType
                        (
                            type: CreateType(param.Type)
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

            if (method is IGenericMethodInfo genericMethod) result = result.WithTypeParameterList // kulon legyen kulonben lesz egy ures "<>"
            (
                typeParameterList: TypeParameterList
                (
                    parameters: genericMethod
                        .GenericArguments
                        .ToSyntaxList
                        (
                            type => TypeParameter
                            (
                                CreateType(type).ToFullString()
                            )
                        )
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
                List<SyntaxKind> modifiers = new(); // lehet tobb is ("internal protected" pl)

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
                    TokenList(modifiers.Convert(Token))
                );
            }

            if (forceInlining) result = result.WithAttributeLists
            (
                attributeLists: DeclareMethodImplAttributeToForceInlining()
            );

            return result;
        }

        /// <summary>
        /// target.Foo(..., ref ..., ...)
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
        /// target.Foo(ref a, b, c)
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
        protected abstract IEnumerable<MethodDeclarationSyntax> ResolveMethods(object context);

        #if DEBUG
        internal
        #endif
        protected abstract MethodDeclarationSyntax ResolveMethod(object context, IMethodInfo method);
    }
}
