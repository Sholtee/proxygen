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

    internal class ProxySyntaxFactory<TInterface, TInterceptor>: ProxySyntaxFactoryBase where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
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

        private static string EnsureUnused(string name, MethodInfo method) 
        {
            for (IReadOnlyList<ParameterInfo> paramz = method.GetParameters(); paramz.Any(param => param.Name == name);)
            {
                name = $"_{name}";
            }
            return name;
        }

        /// <summary>
        /// GeneratedClass.StaticMember
        /// </summary>
        private MemberAccessExpressionSyntax StaticMemberName(SimpleNameSyntax name) =>
            //
            // A generalt osztaly nincs nevter alatt
            //

            MemberAccessExpression
            (
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(GeneratedClassName),
                name
            );

        /// <summary>
        /// GeneratedClass.StaticMember.Member
        /// </summary>
        private MemberAccessExpressionSyntax StaticMemberAccess(SimpleNameSyntax name, string member) =>
            MemberAccessExpression
            (
                SyntaxKind.SimpleMemberAccessExpression,
                StaticMemberName(name),
                IdentifierName(member)
            );
        #endregion

        #region Internal
        /// <summary>
        /// object result = Invoke(...);
        /// </summary>
        internal static LocalDeclarationStatementSyntax CallInvoke(string variableName, params ExpressionSyntax[] arguments) =>
            DeclareLocal<object>(variableName, InvokeMethod
            (
                INVOKE,
                target: null,
                castTargetTo: null,
                arguments: arguments.Select(Argument).ToArray()
            ));

        /// <summary>
        /// object result = Invoke(var1, var2, ..., varN);
        /// </summary>
        internal static LocalDeclarationStatementSyntax CallInvoke(string variableName, params LocalDeclarationStatementSyntax[] arguments) =>
            CallInvoke(variableName, arguments.Select(arg => (ExpressionSyntax)ToIdentifierName(arg)).ToArray());

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

        internal interface IInterceptorFactory
        {
            IEnumerable<MemberDeclarationSyntax> Build();
        }

        /// <summary>
        /// () =>                                                        <br/>
        /// {                                                            <br/>
        ///     System.Int32 cb_a = (System.Int32)args[0];               <br/>
        ///     System.String cb_b;                                      <br/>
        ///     TT cb_c = (TT)args[2];                                   <br/>
        ///     System.Object result;                                    <br/>
        ///     result = this.Target.Foo[TT](cb_a, out cb_b, ref cb_c);  <br/>
        ///                                                              <br/>
        ///     args[1] = (System.Object)cb_b;                           <br/>
        ///     args[2] = (System.Object)cb_c;                           <br/>
        ///     return result;                                           <br/>
        /// };   
        /// </summary>
        internal sealed class CallbackLambdaExpressionFactory
        {
            public MethodInfo Method { get; }

            public LocalDeclarationStatementSyntax ArgsArray { get; }

            public LocalDeclarationStatementSyntax Result { get; }

            public IReadOnlyList<LocalDeclarationStatementSyntax> LocalArgs { get; }

            public CallbackLambdaExpressionFactory(MethodInfo method, LocalDeclarationStatementSyntax argsArray)
            {
                Method    = method;
                ArgsArray = argsArray;
                Result    = DeclareLocal(typeof(object), GetLocalName("result"));
                LocalArgs = DeclareCallbackLocals().ToArray();
            }

            private string GetLocalName(string possibleName) => EnsureUnused(possibleName, Method);

            /// <summary>
            /// System.String cb_a;    <br/>
            /// TT cb_b = (TT)args[1];
            /// </summary>
            internal IEnumerable<LocalDeclarationStatementSyntax> DeclareCallbackLocals()
            {
                IdentifierNameSyntax array = ToIdentifierName(ArgsArray);

                return Method
                    .GetParameters()
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
                            GetLocalName($"cb_{p.Parameter.Name}"),
                            p.Parameter.GetParameterKind() == ParameterKind.Out ? null : CastExpression
                            (
                                type: CreateType(p.Parameter.ParameterType),
                                expression: ElementAccessExpression(array).WithArgumentList
                                (
                                    argumentList: BracketedArgumentList
                                    (
                                        SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(p.Index))))
                                    )
                                )
                            )
                        )
                    );
            }

            /// <summary>
            /// System.Object result = this.Target(...);
            /// 
            /// OR
            /// 
            /// System.Object result = null;
            /// this.Target(...);
            /// </summary>
            internal IEnumerable<StatementSyntax> CallTarget()
            {
                InvocationExpressionSyntax invocation = InvokeMethod(
                    Method,
                    TARGET,
                    castTargetTo: null,
                    arguments: LocalArgs.Select
                    (
                        arg => Argument(ToIdentifierName(arg))
                    ).ToArray());

                yield return ExpressionStatement
                (
                    AssignmentExpression
                    (
                        SyntaxKind.SimpleAssignmentExpression,
                        ToIdentifierName(Result),
                        Method.ReturnType != typeof(void)
                            ? (ExpressionSyntax) invocation
                            : LiteralExpression(SyntaxKind.NullLiteralExpression)
                    )
                );
        
                if (Method.ReturnType == typeof(void))
                    yield return ExpressionStatement(invocation);
            }

            /// <summary>
            /// args[0] = (System.Object)cb_a // ref
            /// args[2] = (TT)cb_c // out
            /// </summary>
            internal IEnumerable<StatementSyntax> ReassignArgsArray()
            {
                IReadOnlyList<ParameterInfo> paramz = Method.GetParameters();

                Debug.Assert(LocalArgs.Count == paramz.Count);

                return Method
                    .GetParameters()
                    .Select((param, i) => new { Parameter = param, Index = i })
                    .Where(p => new[] { ParameterKind.InOut, ParameterKind.Out }.Contains(p.Parameter.GetParameterKind()))
                    .Select
                    (
                        p => ExpressionStatement
                        (
                            expression: AssignmentExpression
                            (
                                kind: SyntaxKind.SimpleAssignmentExpression,
                                left: ElementAccessExpression(ToIdentifierName(ArgsArray)).WithArgumentList
                                (
                                    argumentList: BracketedArgumentList
                                    (
                                        SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(p.Index))))
                                    )
                                ),
                                right: CastExpression
                                (
                                    type: CreateType(typeof(object)),
                                    ToIdentifierName(LocalArgs[p.Index])
                                )
                            )
                        )
                    );

            }

            public LambdaExpressionSyntax Build() => ParenthesizedLambdaExpression
            (
                Block
                (
                    LocalArgs.Cast<StatementSyntax>()
                        .Append(Result)
                        .Concat
                        (
                            CallTarget()
                        )
                        .Concat
                        (
                            ReassignArgsArray()
                        )
                        .Append
                        (
                            ReturnResult(null, Result)
                        )
                )
            );
        }

        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)               <br/>
        /// {                                                                                                    <br/>
        ///     object[] args = new object[] {para1, para2, default(T3), para4};                                 <br/>
        ///                                                                                                      <br/>
        ///     T2 dummy_para2 = default(T2);                                                                    <br/>
        ///     T3 dummy_para3;                                                                                  <br/>
        ///     MethodInfo currentMethod;                                                                        <br/>
        ///     currentMethod = ResolveMethod(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4)); <br/>
        ///                                                                                                      <br/>
        ///     InvokeTarget = () =>                                                                             <br/>
        ///     {                                                                                                <br/>
        ///         System.Int32 cb_a = (System.Int32)args[0];                                                   <br/>
        ///         System.String cb_b;                                                                          <br/>
        ///         TT cb_c = (TT)args[2];                                                                       <br/>
        ///         System.Object result;                                                                        <br/>
        ///         result = this.Target.Foo[TT](cb_a, out cb_b, ref cb_c);                                      <br/>
        ///                                                                                                      <br/>
        ///         args[1] = (System.Object)cb_b;                                                               <br/>
        ///         args[2] = (System.Object)cb_c;                                                               <br/>
        ///         return result;                                                                               <br/>
        ///     };                                                                                               <br/>         
        ///                                                                                                      <br/>
        ///     System.Object result = Invoke(currentMethod, args, currentMethod);                               <br/>
        ///                                                                                                      <br/>
        ///     para2 = (T2) args[1];                                                                            <br/>
        ///     para3 = (T3) args[2];                                                                            <br/>
        ///                                                                                                      <br/>
        ///     return (TResult) result;                                                                         <br/>
        /// }
        /// </summary>
        internal sealed class MethodInterceptorFactory : IInterceptorFactory
        {
            public MethodInfo Method { get; }

            public LocalDeclarationStatementSyntax ArgsArray { get; }

            public LocalDeclarationStatementSyntax CurrentMethod { get; }

            public MethodInterceptorFactory(MethodInfo method) 
            {
                //
                // "ref" visszateres nem tamogatott.
                //

                if (method.ReturnType.IsByRef)
                    throw new NotSupportedException(Resources.REF_RETURNS_NOT_SUPPORTED);

                Method = method;
                ArgsArray = CreateArgumentsArray();
                CurrentMethod = DeclareLocal<MethodInfo>(GetLocalName("currentMethod"));
            }

            private string GetLocalName(string possibleName) => EnsureUnused(possibleName, Method);

            /// <summary>
            /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)  <br/>
            /// {                                                                                       <br/>
            ///   ...                                                                                   <br/>
            ///   object[] args = new object[]{para1, para2, default(T3), para4};                       <br/>
            ///   ...                                                                                   <br/>
            /// }
            /// </summary>
            internal LocalDeclarationStatementSyntax CreateArgumentsArray() => DeclareLocal<object[]>(GetLocalName("args"), CreateArray<object>(Method
                .GetParameters()
                .Select(param => param.IsOut 
                    ? DefaultExpression(CreateType(param.ParameterType)) 
                    : (ExpressionSyntax) IdentifierName(param.Name))
                .ToArray()));

            /// <summary>
            /// T2 dummy_para2 = default(T2);                                                                    <br/>
            /// T3 dummy_para3;                                                                                  <br/>
            /// currentMethod = ResolveMethod(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4));
            /// </summary>
            internal IEnumerable<StatementSyntax> AcquireMethodInfo()
            {
                IReadOnlyList<ParameterInfo> paramz = Method.GetParameters();

                var statements = new List<StatementSyntax>();

                //
                // T2 dummy_para2 = default(T2);
                // T3 dummy_para3;
                //

                statements.AddRange
                (
                    paramz.Where(param => param.ParameterType.IsByRef).Select(param => DeclareLocal
                    (
                        type: param.ParameterType,
                        name: GetDummyName(param),
                        initializer: param.IsOut ? null : DefaultExpression
                        (
                            type: CreateType(param.ParameterType)
                        )
                    ))
                );

                //
                // urrentMethod = ResolveMethod(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4));
                //

                statements.Add
                (
                    ExpressionStatement
                    (
                        AssignmentExpression
                        (
                            SyntaxKind.SimpleAssignmentExpression, 
                            ToIdentifierName(CurrentMethod),
                            InvokeMethod
                            (
                                RESOLVE_METHOD,
                                target: null,
                                castTargetTo: null,
                                Argument
                                (
                                    expression: ParenthesizedLambdaExpression
                                    (
                                        parameterList: ParameterList(), // Roslyn 3.4.0 felrobban ha nincs parameter lista (3.3.X-nel meg opcionalis volt)
                                        body: InvokeMethod
                                        (
                                            Method, 
                                            TARGET,
                                            castTargetTo: null,

                                            //
                                            // GetDummyName() azert kell mert ByRef parameterek nem szerepelhetnek kifejezesekben.
                                            //

                                            arguments: paramz.Select(param => param.ParameterType.IsByRef ? GetDummyName(param) : param.Name).ToArray()
                                        )
                                    )
                                )
                            )
                        )
                    )
                );

                return statements;

                string GetDummyName(ParameterInfo param) => GetLocalName($"dummy_{param.Name}");
            }

            /// <summary>
            /// InvokeTarget = () => { ... };
            /// </summary>
            internal StatementSyntax AssignCallback() => ExpressionStatement
            (
                expression: AssignmentExpression
                (
                    kind: SyntaxKind.SimpleAssignmentExpression,
                    left: PropertyAccess(INVOKE_TARGET, null),
                    right: new CallbackLambdaExpressionFactory(Method, ArgsArray).Build()
                )
            );

            /// <summary>
            /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)   <br/>
            /// {                                                                                        <br/>
            ///   ...                                                                                    <br/>
            ///   para2 = (T2) args[1];                                                                  <br/>
            ///   para3 = (T3) args[2];                                                                  <br/>
            ///   ...                                                                                    <br/>
            /// }
            /// </summary>
            internal IEnumerable<ExpressionStatementSyntax> AssignByRefParameters() => Method
                .GetParameters()
                .Select((param, i) => new { Parameter = param, Index = i })
                .Where(p => new[] { ParameterKind.InOut, ParameterKind.Out }.Contains(p.Parameter.GetParameterKind()))
                .Select
                (
                    p => ExpressionStatement
                    (
                        expression: AssignmentExpression
                        (
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: IdentifierName(p.Parameter.Name),
                            right: CastExpression
                            (
                                type: CreateType(p.Parameter.ParameterType),
                                expression: ElementAccessExpression(ToIdentifierName(ArgsArray)).WithArgumentList
                                (
                                    argumentList: BracketedArgumentList
                                    (
                                        SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(p.Index))))
                                    )
                                )
                            )
                        )
                    )
                );

            public IEnumerable<MemberDeclarationSyntax> Build() 
            {
                LocalDeclarationStatementSyntax result = CallInvoke(GetLocalName("result"), CurrentMethod, ArgsArray, CurrentMethod);

                IEnumerable<StatementSyntax> statements = new List<StatementSyntax>()
                    .Append(ArgsArray)
                    .Append(CurrentMethod)
                    .Concat(AcquireMethodInfo())
                    .Append(AssignCallback())
                    .Append(result)
                    .Concat(AssignByRefParameters());

                if (Method.ReturnType != typeof(void)) statements = statements.Append
                (
                    ReturnResult(Method.ReturnType, result)
                );

                yield return DeclareMethod(Method).WithBody
                (
                    body: Block
                    (
                        statements: List(statements)
                    )
                );
            }
        }

        /// <summary>
        /// TResult IInterface.Prop                                                          <br/>
        /// {                                                                                <br/>
        ///     get                                                                          <br/>
        ///     {                                                                            <br/>
        ///         InvokeTarget = () => Target.Prop;                                        <br/>
        ///         PropertyInfo prop = ResolveProperty(InvokeTarget);                       <br/>
        ///         return (TResult) Invoke(prop.GetMethod, new object[0], prop);            <br/>
        ///     }                                                                            <br/>
        ///     set                                                                          <br/>
        ///     {                                                                            <br/>
        ///         InvokeTarget = () =>                                                     <br/>
        ///         {                                                                        <br/>
        ///           Target.Prop = value;                                                   <br/>
        ///           return null;                                                           <br/>
        ///         };                                                                       <br/>
        ///         PropertyInfo prop = ResolveProperty(InvokeTarget);                       <br/>
        ///         Invoke(prop.SetMethod, new object[]{ value }, prop);                     <br/>
        ///     }                                                                            <br/>
        /// }
        /// </summary>
        internal class PropertyInterceptorFactory : IInterceptorFactory
        {
            public PropertyInfo Property { get; }

            public ProxySyntaxFactory<TInterface, TInterceptor> Owner { get; }

            public PropertyInterceptorFactory(PropertyInfo property, ProxySyntaxFactory<TInterface, TInterceptor> owner) 
            {
                Property = property;
                Owner = owner;
            }

            /// <summary>
            /// () => Target.Prop;
            /// </summary>
            internal LambdaExpressionSyntax BuildPropertyGetter() => ParenthesizedLambdaExpression
            (
                PropertyAccess(Property, TARGET)
            );

            /// <summary>
            /// () =>                     <br/>
            /// {                         <br/>
            ///     Target.Prop = value;  <br/>
            ///     return null;          <br/>
            /// };  
            /// </summary>
            internal LambdaExpressionSyntax BuildPropertySetter() => ParenthesizedLambdaExpression
            (
                Block
                (
                    ExpressionStatement
                    (
                        expression: AssignmentExpression
                        (
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: PropertyAccess(Property, TARGET),
                            right: IdentifierName(Value)
                        )
                    ),
                    ReturnStatement
                    (
                        LiteralExpression(SyntaxKind.NullLiteralExpression)
                    )
                )
            );

            /// <summary>
            /// PropertyInfo prop = ResolveProperty(InvokeTarget);                 <br/>
            /// return (TResult) this.Invoke(prop.GetMethod, new object[0], prop);
            /// </summary>
            internal IEnumerable<StatementSyntax> CallInvokeAndReturn(params ParameterSyntax[] paramz) 
            {
                LocalDeclarationStatementSyntax prop = DeclareLocal(typeof(PropertyInfo), "prop", InvokeMethod
                (
                    RESOLVE_PROPERTY,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        expression: PropertyAccess(INVOKE_TARGET, null)
                    )
                ));

                yield return prop;

                yield return ReturnResult
                (
                    Property.PropertyType,
                    InvokeMethod
                    (
                        INVOKE,
                        target: null,
                        castTargetTo: null,
                        arguments: new ExpressionSyntax[]
                        {
                            SimpleMemberAccess // prop.GetMethod
                            (
                                ToIdentifierName(prop),  
                                nameof(PropertyInfo.GetMethod)
                            ), 
                            CreateArray<object>(paramz // new object[0] | new object[] {index1, index2, ...}
                                .Select(param => IdentifierName(param.Identifier))
                                .Cast<ExpressionSyntax>()
                                .ToArray()),
                            ToIdentifierName(prop) // prop
                        }.Select(Argument).ToArray()
                    )
                );
            }

            /// <summary>
            /// PropertyInfo prop = ResolvePropertySet(InvokeTarget);     <br/>
            /// this.Invoke(prop.SetMethod, new object[]{ value }, prop);
            /// </summary>
            internal IEnumerable<StatementSyntax> CallInvoke(params ParameterSyntax[] paramz)
            {
                LocalDeclarationStatementSyntax prop = DeclareLocal(typeof(PropertyInfo), nameof(prop), InvokeMethod
                (
                    RESOLVE_PROPERTY,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        expression: PropertyAccess(INVOKE_TARGET, null)
                    )
                ));

                yield return prop;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        INVOKE,
                        target: null,
                        castTargetTo: null,
                        arguments: new ExpressionSyntax[]
                        {
                            SimpleMemberAccess // prop.SetMethod
                            (
                                ToIdentifierName(prop),
                                nameof(PropertyInfo.SetMethod)
                            ),
                            CreateArray<object>(paramz //  new object[] {value} | new object[] {index1, index2, ..., value}
                                .Select(param => IdentifierName(param.Identifier))
                                .Append(IdentifierName(Value))
                                .Cast<ExpressionSyntax>()
                                .ToArray()),
                            ToIdentifierName(prop) // prop
                        }.Select(Argument).ToArray()
                    )
                );
            }

            /// <summary>
            /// InvokeTarget = () => { ... };
            /// </summary>
            protected static StatementSyntax AssignCallback(LambdaExpressionSyntax lambda) => ExpressionStatement
            (
                expression: AssignmentExpression
                (
                    kind: SyntaxKind.SimpleAssignmentExpression,
                    left: PropertyAccess(INVOKE_TARGET, null),
                    right: lambda
                )
            );

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            protected virtual MemberDeclarationSyntax DeclareProperty() => ProxySyntaxFactoryBase.DeclareProperty
            (
                property: Property,
                getBody: Block
                (
                    new StatementSyntax[]
                    {
                        AssignCallback(BuildPropertyGetter())
                    }.Concat(CallInvokeAndReturn())
                ),
                setBody: Block
                (
                    new StatementSyntax[]
                    {
                        AssignCallback(BuildPropertySetter())
                    }.Concat(CallInvoke())
                )
            );

            public IEnumerable<MemberDeclarationSyntax> Build() 
            {
                yield return DeclareProperty();
            }
        }

        /// <summary>
        /// TResult IInterface.this[TParam1 p1, TPAram2 p2]                                         <br/>
        /// {                                                                                       <br/>
        ///     get                                                                                 <br/>
        ///     {                                                                                   <br/>
        ///         InvokeTarget = () => Target.Prop[p1, p2];                                       <br/>
        ///         PropertyInfo prop = ResolveProperty(InvokeTarget);                              <br/>
        ///         return (TResult) Invoke(prop.GetMethod, new System.Object[]{p1, p2}, prop);     <br/>
        ///     }                                                                                   <br/>
        ///     set                                                                                 <br/>
        ///     {                                                                                   <br/>
        ///         InvokeTarget = () =>                                                            <br/>
        ///         {                                                                               <br/>
        ///           Target.Prop[p1, p2] = value;                                                  <br/>
        ///           return null;                                                                  <br/>
        ///         };                                                                              <br/>
        ///         PropertyInfo prop = ResolveProperty(InvokeTarget);                              <br/>
        ///         Invoke(prop.SetMethod, new System.Object[]{ p1, p2, value }, prop);             <br/>
        ///     }                                                                                   <br/>
        /// }
        /// </summary>
        internal sealed class IndexedPropertyInterceptorFactory : PropertyInterceptorFactory
        {
            public IndexedPropertyInterceptorFactory(PropertyInfo property, ProxySyntaxFactory<TInterface, TInterceptor> owner) : base(property, owner)
            {
                Debug.Assert(property.IsIndexer());
            }

            protected override MemberDeclarationSyntax DeclareProperty() => DeclareIndexer
            (
                property: Property,
                getBody: paramz => Block
                (
                    new StatementSyntax[]
                    {
                        AssignCallback(BuildPropertyGetter())
                    }.Concat(CallInvokeAndReturn(paramz.ToArray()))
                ),
                setBody: paramz => Block
                (
                    new StatementSyntax[]
                    {
                        AssignCallback(BuildPropertySetter())
                    }.Concat(CallInvoke(paramz.ToArray()))
                )
            );
        }

        /// <summary>
        /// event EventType IInterface.Event                                            <br/>
        /// {                                                                           <br/>
        ///     add                                                                     <br/>
        ///     {                                                                       <br/>
        ///         InvokeTarget = () => Target.Event += value;                         <br/>
        ///         EventInfo evt = ResolveEvent(InvokeTarget);                         <br/>
        ///         Invoke(evt.AddMethod, new object[]{ value }, evt);                  <br/>
        ///     }                                                                       <br/>
        ///     remove                                                                  <br/>
        ///     {                                                                       <br/>
        ///         InvokeTarget = () => Target.Event -= value;                         <br/>
        ///         EventInfo evt = ResolveEvent(InvokeTarget);                         <br/>
        ///         Invoke(evt.RemoveMethod, new object[]{ value }, evt);               <br/>
        ///     }                                                                       <br/>
        /// }
        /// </summary>
        internal sealed class EventInterceptorFactory : IInterceptorFactory
        {
            public EventInfo Event { get; }

            public ProxySyntaxFactory<TInterface, TInterceptor> Owner { get; }

            public EventInterceptorFactory(EventInfo @event, ProxySyntaxFactory<TInterface, TInterceptor> owner)
            {
                Event = @event;
                Owner = owner;
            }

            /// <summary>
            /// () => 
            /// {
            ///   Target.Event [+|-]= value;
            ///   return null;
            /// }
            /// </summary>
            internal LambdaExpressionSyntax BuildRegister(bool add) => ParenthesizedLambdaExpression
            (
                Block
                (
                    ExpressionStatement
                    (
                        RegisterEvent(Event, TARGET, add)
                    ),
                    ReturnStatement
                    (
                        LiteralExpression(SyntaxKind.NullLiteralExpression)
                    )
                )
            );

            /// <summary>
            /// EventInfo evt = ResolveEvent(InvokeTarget);                    <br/>
            /// this.Invoke(evt.AddMethod, new System.Object[]{ value }, evt);
            /// </summary>
            internal IEnumerable<StatementSyntax> CallInvoke(bool add)
            {
                LocalDeclarationStatementSyntax evt = DeclareLocal(typeof(EventInfo), nameof(evt), InvokeMethod
                (
                    RESOLVE_EVENT,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        expression: PropertyAccess(INVOKE_TARGET, null)
                    )
                ));

                yield return evt;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        INVOKE,
                        target: null,
                        castTargetTo: null,
                        arguments: new ExpressionSyntax[]
                        {
                            SimpleMemberAccess // evt.[Add|Remove]Method
                            (
                                ToIdentifierName(evt),
                                add ? nameof(EventInfo.AddMethod) : nameof(EventInfo.RemoveMethod)
                            ),
                            CreateArray<object>(IdentifierName(Value)),
                            ToIdentifierName(evt) // evt
                        }.Select(Argument).ToArray()
                    )
                );
            }

            /// <summary>
            /// InvokeTarget = () => ...;
            /// </summary>
            private static StatementSyntax AssignCallback(LambdaExpressionSyntax lambda) => ExpressionStatement
            (
                expression: AssignmentExpression
                (
                    kind: SyntaxKind.SimpleAssignmentExpression,
                    left: PropertyAccess(INVOKE_TARGET, null),
                    right: lambda
                )
            );

            public IEnumerable<MemberDeclarationSyntax> Build()
            {
                yield return DeclareEvent
                (
                    @event: Event,
                    addBody: Block
                    (
                        new StatementSyntax[]
                        {
                            AssignCallback(BuildRegister(add: true))
                        }.Concat(CallInvoke(add: true))
                    ),
                    removeBody: Block
                    (
                        new StatementSyntax[]
                        {
                            AssignCallback(BuildRegister(add: false))
                        }.Concat(CallInvoke(add: false))
                    )
                );
            }
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
                p => p.IsIndexer() ? new IndexedPropertyInterceptorFactory(p, this) : new PropertyInterceptorFactory(p, this));
            DeclareMembers<EventInfo>(
                e => true,
                e => new EventInterceptorFactory(e, this));

            return cls.WithMembers(List(members));

            void DeclareMembers<TMember>(Func<TMember, bool> filter, Func<TMember, IInterceptorFactory> interceptorFactory) where TMember : MemberInfo => members.AddRange
            (
                interfaceType
                    .ListMembers<TMember>()

                    //
                    // Az interceptor altal mar implementalt interface-ek ne szerepeljenek a proxy deklaracioban.
                    //

                    .Where(m => !implementedInterfaces.Contains(m.DeclaringType) && filter(m))
                    .SelectMany
                    (
                        member => interceptorFactory(member).Build()
                    )
            );
        }
    }
}