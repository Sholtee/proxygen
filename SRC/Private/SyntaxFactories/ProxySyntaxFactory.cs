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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class ProxySyntaxFactory<TInterface, TInterceptor>: ProxySyntaxFactoryBase where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        #region Private
        private static readonly MemberAccessExpressionSyntax
            //
            // this.Target
            //
            TARGET = MemberAccess(null, MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.Target!));

        private static readonly MethodInfo
            INVOKE           = (MethodInfo) MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.Invoke(default!, default!, default!)),
            RESOLVE_METHOD   = (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.ResolveMethod(default!)),
            RESOLVE_PROPERTY = (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.ResolveProperty(default!)),
            RESOLVE_EVENT    = (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.ResolveEvent(default!));

        private static readonly PropertyInfo
            INVOKE_TARGET = (PropertyInfo) MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.InvokeTarget!);

        private static string EnsureUnused(string name, IEnumerable<ParameterInfo> parameters) 
        {
            while (parameters.Any(param => param.Name == name))
            {
                name = $"_{name}";
            }
            return name;
        }
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
        internal static LocalDeclarationStatementSyntax CreateArgumentsArray(MethodInfo method)
        {
            ParameterInfo[] paramz = method.GetParameters();

            return DeclareLocal<object[]>(EnsureUnused("args", paramz), CreateArray<object>(paramz
                .Select(param => param.IsOut
                    ? DefaultExpression(CreateType(param.ParameterType))
                    : (ExpressionSyntax) IdentifierName(param.Name))
                .ToArray()));
        }

        /// <summary>
        /// return;          <br/>
        ///                  <br/>
        /// OR               <br/>
        ///                  <br/>
        /// return (T) ...;
        /// </summary>
        internal static ReturnStatementSyntax ReturnResult(Type? returnType, ExpressionSyntax result) => ReturnStatement
        (
            expression: returnType == typeof(void)
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
        internal static ReturnStatementSyntax ReturnResult(Type? returnType, LocalDeclarationStatementSyntax result) =>
            ReturnResult(returnType, ToIdentifierName(result));


        /// <summary>
        /// InvokeTarget = ...;
        /// </summary>
        internal static StatementSyntax AssignCallback(LambdaExpressionSyntax lambda) => ExpressionStatement
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
        internal static IEnumerable<LocalDeclarationStatementSyntax> DeclareCallbackLocals(LocalDeclarationStatementSyntax argsArray, IEnumerable<ParameterInfo> paramz) => paramz
            .Select((param, i) => new { Parameter = param, Index = i })

            //
            // Az osszes parametert az "args" tombbol vesszuk mert lehet az Invoke() override-ja modositana vmelyik bemeno
            // erteket.
            //

            .Select
            (
                p => DeclareLocal
                (
                    p.Parameter.ParameterType,
                    EnsureUnused($"cb_{p.Parameter.Name}", paramz),
                    p.Parameter.GetParameterKind() == ParameterKind.Out ? null : CastExpression
                    (
                        type: CreateType(p.Parameter.ParameterType),
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
            );

        /// <summary>
        /// () =>                                          <br/>
        /// {                                              <br/>
        ///     System.Int32 cb_a = (System.Int32)args[0]; <br/>
        ///     System.String cb_b;                        <br/>
        ///     TT cb_c = (TT)args[2];                     <br/>
        ///     System.Object result;                      <br/>
        ///     result = ...;                              <br/>
        ///     return result;                             <br/>
        /// };   
        /// </summary>
        internal static LambdaExpressionSyntax DeclareCallback(LocalDeclarationStatementSyntax argsArray, IEnumerable<ParameterInfo> paramz, Func<IReadOnlyList<LocalDeclarationStatementSyntax>, LocalDeclarationStatementSyntax, IEnumerable<StatementSyntax>> invocationFactory)
        {
            IReadOnlyList<LocalDeclarationStatementSyntax> locals = DeclareCallbackLocals(argsArray, paramz).ToArray();

            LocalDeclarationStatementSyntax result = DeclareLocal(typeof(object), EnsureUnused(nameof(result), paramz));

            var statements = new List<StatementSyntax>();
            statements.AddRange(locals);
            statements.Add(result);
            statements.AddRange
            (
                invocationFactory(locals, result)
            );
            statements.Add
            (
                ReturnResult(null, result)
            );

            return ParenthesizedLambdaExpression
            (
                Block(statements)
            );
        }

        internal interface IInterceptorFactory
        {
            MemberDeclarationSyntax Build();
        }
        #endregion

        public override string AssemblyName => $"{GetSafeTypeName<TInterceptor>()}_{GetSafeTypeName<TInterface>()}_Proxy"; 

        protected internal override ClassDeclarationSyntax GenerateProxyClass()
        {
            Type
                interfaceType   = typeof(TInterface),
                interceptorType = typeof(TInterceptor);

            Debug.Assert(interfaceType.IsInterface);

            ClassDeclarationSyntax cls = ClassDeclaration(GeneratedClassName)
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
                    new[] { interceptorType, interfaceType }.ToSyntaxList(t => (BaseTypeSyntax) SimpleBaseType(CreateType(t)))
                )
            );

            HashSet<Type> implementedInterfaces = new HashSet<Type>(interceptorType.GetInterfaces());

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>
            (
                interceptorType.GetPublicConstructors().Select(DeclareCtor)
            );

            DeclareMembers<MethodInfo>(
                m => !m.IsSpecialName, 
                m => new MethodInterceptorFactory(m));
            DeclareMembers<PropertyInfo>(
                p => true,
                p => p.IsIndexer() ? new IndexerInterceptorFactory(p) : new PropertyInterceptorFactory(p));
            DeclareMembers<EventInfo>(
                e => true,
                e => new EventInterceptorFactory(e));

            return cls.WithMembers(List(members));

            void DeclareMembers<TMember>(Func<TMember, bool> filter, Func<TMember, IInterceptorFactory> interceptorFactory) where TMember : MemberInfo => members.AddRange
            (
                interfaceType
                    .ListMembers<TMember>()

                    //
                    // Az interceptor altal mar implementalt interface-ek ne szerepeljenek a proxy deklaracioban.
                    //

                    .Where(m => !implementedInterfaces.Contains(m.DeclaringType) && filter(m))
                    .Select
                    (
                        member => interceptorFactory(member).Build()
                    )
            );
        }
    }
}