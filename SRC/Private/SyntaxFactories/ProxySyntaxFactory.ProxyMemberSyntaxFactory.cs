﻿/********************************************************************************
* ProxySyntaxFactory.ProxyMemberSyntaxFactory.cs                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
        internal abstract class ProxyMemberSyntaxFactory: MemberSyntaxFactory
        {
            #region Protected
            protected internal readonly IPropertyInfo
                TARGET,
                INVOKE_TARGET;

            protected internal readonly IMethodInfo 
                INVOKE;

            protected internal static string EnsureUnused(string name, IEnumerable<IParameterInfo> parameters) 
            {
                while (parameters.Any(param => param.Name == name))
                {
                    name = $"_{name}";
                }
                return name;
            }

            protected internal static string EnsureUnused(string name, IMethodInfo method) => EnsureUnused(name, method.Parameters);

            //
            // Az interceptor altal mar implementalt interface-ek ne szerepeljenek a proxy deklaracioban.
            //

            protected bool AlreadyImplemented(IMemberInfo member) => Context
                .InterceptorType
                .Interfaces
                .Any(iface => iface.EqualsTo(member.DeclaringType));

            /// <summary>
            /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)  <br/>
            /// {                                                                                       <br/>
            ///   ...                                                                                   <br/>
            ///   object[] args = new object[]{para1, para2, default(T3), para4};                       <br/>
            ///   ...                                                                                   <br/>
            /// }
            /// </summary>
            protected internal LocalDeclarationStatementSyntax CreateArgumentsArray(IMethodInfo method)
            {
                IReadOnlyList<IParameterInfo> paramz = method.Parameters;

                return DeclareLocal<object[]>
                (
                    EnsureUnused("args", paramz), CreateArray<object>
                    (
                        paramz
                            .Select(param => param.Kind == ParameterKind.Out
                                ? DefaultExpression(CreateType(param.Type))
                                : (ExpressionSyntax) IdentifierName(param.Name))
                            .ToArray()
                    )
                );
            }

            /// <summary>
            /// return;          <br/>
            ///                  <br/>
            /// OR               <br/>
            ///                  <br/>
            /// return (T) ...;
            /// </summary>
            protected internal ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, ExpressionSyntax result) => ReturnStatement
            (
                expression: returnType?.IsVoid == true
                    ? null
                    : returnType != null
                        ? CastExpression
                        (
                            type: CreateType(returnType),
                            expression: result
                        )
                        : result
            );

            /// <summary>
            /// return;             <br/>
            ///                     <br/>
            /// OR                  <br/>
            ///                     <br/>
            /// return (T) result;
            /// </summary>
            protected internal ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, LocalDeclarationStatementSyntax result) =>
                ReturnResult(returnType, ToIdentifierName(result));

            /// <summary>
            /// return null;
            /// </summary>
            protected static internal ReturnStatementSyntax ReturnNull() => ReturnStatement
            (
                LiteralExpression(SyntaxKind.NullLiteralExpression)
            );

            /// <summary>
            /// InvokeTarget = ...;
            /// </summary>
            protected internal StatementSyntax AssignCallback(ExpressionSyntax expr) => ExpressionStatement
            (
                expression: AssignmentExpression
                (
                    kind: SyntaxKind.SimpleAssignmentExpression,
                    left: PropertyAccess(INVOKE_TARGET, null, null),
                    right: expr
                )
            );

            /// <summary>
            /// System.String cb_a;    <br/>
            /// TT cb_b = (TT)args[1];
            /// </summary>
            protected internal LocalDeclarationStatementSyntax[] DeclareCallbackLocals(LocalDeclarationStatementSyntax argsArray, IEnumerable<IParameterInfo> paramz) => paramz
                .Select((param, i) => new { Parameter = param, Index = i })

                //
                // Az osszes parametert az "args" tombbol vesszuk mert lehet az Invoke() override-ja modositana vmelyik bemeno
                // erteket.
                //

                .Select
                (
                    p => DeclareLocal
                    (
                        p.Parameter.Type,
                        EnsureUnused($"cb_{p.Parameter.Name}", paramz),
                        p.Parameter.Kind == ParameterKind.Out ? null : CastExpression
                        (
                            type: CreateType(p.Parameter.Type),
                            expression: ElementAccessExpression(ToIdentifierName(argsArray)).WithArgumentList
                            (
                                argumentList: BracketedArgumentList
                                (
                                    SingletonSeparatedList
                                    (
                                        Argument
                                        (
                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(p.Index))
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
                .ToArray();

            /// <summary>
            /// () =>                                          <br/>
            /// {                                              <br/>
            ///     System.Int32 cb_a = (System.Int32)args[0]; <br/>
            ///     System.String cb_b;                        <br/>
            ///     TT cb_c = (TT)args[2];                     <br/>
            ///     ...                                        <br/>
            /// };
            /// </summary>
            protected internal LambdaExpressionSyntax DeclareCallback(LocalDeclarationStatementSyntax argsArray, IMethodInfo method, Action<IReadOnlyList<LocalDeclarationStatementSyntax>, List<StatementSyntax>> invocationFactory)
            {
                IReadOnlyList<IParameterInfo> paramz = method.Parameters;

                var statements = new List<StatementSyntax>();

                IReadOnlyList<LocalDeclarationStatementSyntax> locals = DeclareCallbackLocals(argsArray, paramz);
                statements.AddRange(locals);

                invocationFactory(locals, statements);

                return ParenthesizedLambdaExpression
                (
                    Block(statements)
                );
            }
            
            protected abstract IEnumerable<MemberDeclarationSyntax> BuildMembers(CancellationToken cancellation);
            #endregion

            public sealed override bool Build(CancellationToken cancellation)
            {
                if (Members is not null) return false;

                Members = BuildMembers(cancellation).ToArray();

                return true;
            }

            public IProxyContext Context { get; }

            public ProxyMemberSyntaxFactory(IProxyContext context) : base(context.InterfaceType)
            {
                Context = context;

                TARGET = Context.InterceptorType.Properties.Single
                (
                    prop => prop.Name == nameof(InterfaceInterceptor<object>.Target)
                );

                INVOKE_TARGET = Context.InterceptorType.Properties.Single
                (
                    prop => prop.Name == nameof(InterfaceInterceptor<object>.InvokeTarget)
                );

                INVOKE = Context.InterceptorType.Methods.Single
                (
                    met => met.SignatureEquals
                    (
                        MetadataMethodInfo.CreateFrom
                        (
                            (MethodInfo) MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<object>>(ic => ic.Invoke(default!, default!, default!))
                        )
                    )
                );
            }
        }
    }
}