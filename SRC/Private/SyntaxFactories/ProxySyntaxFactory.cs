/********************************************************************************
* ProxySyntaxFactory.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory<TInterface, TInterceptor>: ProxySyntaxFactoryBase where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        #region Private
        //
        // this.Target
        //

        private readonly MemberAccessExpressionSyntax TARGET;

        private static readonly IMethodInfo
            INVOKE = MetadataMethodInfo.CreateFrom((MethodInfo) MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.Invoke(default!, default!, default!)));

        private static readonly IPropertyInfo
            INVOKE_TARGET = MetadataPropertyInfo.CreateFrom((PropertyInfo) MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.InvokeTarget!));

        private static string EnsureUnused(string name, IEnumerable<IParameterInfo> parameters) 
        {
            while (parameters.Any(param => param.Name == name))
            {
                name = $"_{name}";
            }
            return name;
        }

        private static string EnsureUnused(string name, IMethodInfo method) => EnsureUnused(name, method.Parameters);
        #endregion

        #region Internal
        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)  <br/>
        /// {                                                                                       <br/>
        ///   ...                                                                                   <br/>
        ///   object[] args = new object[]{para1, para2, default(T3), para4};                       <br/>
        ///   ...                                                                                   <br/>
        /// }
        /// </summary>
        internal LocalDeclarationStatementSyntax CreateArgumentsArray(IMethodInfo method)
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
        internal ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, ExpressionSyntax result) => ReturnStatement
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
        internal ReturnStatementSyntax ReturnResult(ITypeInfo? returnType, LocalDeclarationStatementSyntax result) =>
            ReturnResult(returnType, ToIdentifierName(result));


        /// <summary>
        /// InvokeTarget = ...;
        /// </summary>
        internal StatementSyntax AssignCallback(LambdaExpressionSyntax lambda) => ExpressionStatement
        (
            expression: AssignmentExpression
            (
                kind: SyntaxKind.SimpleAssignmentExpression,
                left: PropertyAccess(INVOKE_TARGET, null, null),
                right: lambda
            )
        );

        /// <summary>
        /// System.String cb_a;    <br/>
        /// TT cb_b = (TT)args[1];
        /// </summary>
        internal LocalDeclarationStatementSyntax[] DeclareCallbackLocals(LocalDeclarationStatementSyntax argsArray, IEnumerable<IParameterInfo> paramz) => paramz
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
        ///                                                <br/>
        ///     System.Object result;                      <br/>
        ///     result = ...;                              <br/>
        ///     return result;                             <br/>
        ///                                                <br/>
        ///     OR                                         <br/>
        ///                                                <br/>
        ///     ...;                                       <br/>
        ///     return null;                               <br/>
        /// };   
        /// </summary>
        internal LambdaExpressionSyntax DeclareCallback(LocalDeclarationStatementSyntax argsArray, IMethodInfo method, Func<IReadOnlyList<LocalDeclarationStatementSyntax>, LocalDeclarationStatementSyntax?, IEnumerable<StatementSyntax>> invocationFactory)
        {
            IReadOnlyList<IParameterInfo> paramz = method.Parameters;

            var statements = new List<StatementSyntax>();

            IReadOnlyList<LocalDeclarationStatementSyntax> locals = DeclareCallbackLocals(argsArray, paramz);
            statements.AddRange(locals);

            if (!method.ReturnValue.Type.IsVoid)
            {
                LocalDeclarationStatementSyntax result = DeclareLocal<object>(EnsureUnused(nameof(result), paramz));

                statements.Add(result);
                statements.AddRange
                (
                    invocationFactory(locals, result)
                );
                statements.Add
                (
                    ReturnResult(null, result)
                );
            }
            else 
            {
                statements.AddRange
                (
                    invocationFactory(locals, null)
                );
                statements.Add
                (
                    ReturnStatement
                    (
                        LiteralExpression(SyntaxKind.NullLiteralExpression)
                    )
                );
            }

            return ParenthesizedLambdaExpression
            (
                Block(statements)
            );
        }
        #endregion

        public override string AssemblyName => $"{GetSafeTypeName<TInterceptor>()}_{GetSafeTypeName<TInterface>()}_Proxy";

        public ProxySyntaxFactory() =>
            TARGET = MemberAccess(null, MetadataPropertyInfo.CreateFrom((PropertyInfo) MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.Target!)));

        protected override MemberDeclarationSyntax GenerateProxyClass(CancellationToken cancellation)
        {
            ITypeInfo
                interfaceType   = MetadataTypeInfo.CreateFrom(typeof(TInterface)),
                interceptorType = MetadataTypeInfo.CreateFrom(typeof(TInterceptor));

            Debug.Assert(interfaceType.IsInterface);

            ClassDeclarationSyntax cls = ClassDeclaration(ProxyClassName)
            .WithModifiers
            (
                modifiers: TokenList
                (
                    //
                    // Az osztaly ne publikus legyen h "internal" lathatosagu tipusokat is hasznalhassunk
                    //

                    Token(SyntaxKind.InternalKeyword),
                    Token(SyntaxKind.SealedKeyword)
                )
            )
            .WithBaseList
            (
                baseList: BaseList
                (
                    new[] { interceptorType, interfaceType }.ToSyntaxList
                    (
                        t => (BaseTypeSyntax) SimpleBaseType
                        (
                            CreateType(t)
                        )
                    )
                )
            );

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>
            (
                interceptorType.Constructors.Select(DeclareCtor)
            );

            members.AddRange(BuildMembers<MethodInterceptorFactory>(interfaceType.Methods, cancellation));
            members.AddRange(BuildMembers<PropertyInterceptorFactory>(interfaceType.Properties, cancellation));
            members.AddRange(BuildMembers<IndexerInterceptorFactory>(interfaceType.Properties, cancellation));
            members.AddRange(BuildMembers<EventInterceptorFactory>(interfaceType.Events, cancellation));

            return cls.WithMembers
            (
                List(members)
            );
        }
    }
}