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
            TARGET = MemberAccess(null, MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.Target!)),
            //
            // this.CALL_TARGET
            //
            CALL_TARGET = MemberAccess(null, MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.CALL_TARGET));

        private static readonly MethodInfo
            INVOKE = (MethodInfo) MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.Invoke(default!, default!, default!)),
            METHOD_ACCESS = (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.MethodAccess(default!));

        private static readonly FieldInfo
            EVENTS = (FieldInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.Events),
            PROPERTIES = (FieldInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.Properties);

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

        private static IdentifierNameSyntax GenerateFieldName<TMember>(TMember current) where TMember: MemberInfo
        {
            var generator = typeof(TInterface)
                .ListMembers<TMember>()
                .Where(member => member.Name == current.Name)
                .Select((member, i) => new { Index = i, Value = member })
                .First(member => member.Value == current);

            return IdentifierName($"F{generator.Value.Name}{generator.Index}");
        }

        private static FieldDeclarationSyntax DeclareField<TFiled>(IdentifierNameSyntax fieldName, MemberInfo getValueFrom, MemberInfo key) =>
            DeclareField<TFiled>
            (
                name: fieldName.Identifier.Text,
                initializer: ElementAccess
                (
                    null, // target
                    getValueFrom
                )
                .WithArgumentList
                (
                    argumentList: BracketedArgumentList
                    (
                        arguments: SingletonSeparatedList
                        (
                            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(key.GetFullName())))
                        )
                    )
                ),
                modifiers: new[]
                {
                    SyntaxKind.PrivateKeyword,
                    SyntaxKind.StaticKeyword,
                    SyntaxKind.ReadOnlyKeyword
                }
            );

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
        public sealed class CallbackLambdaExpressionFactory
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
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)              <br/>
        /// {                                                                                                   <br/>
        ///     object[] args = new object[] {para1, para2, default(T3), para4};                                <br/>
        ///                                                                                                     <br/>
        ///     T2 dummy_para2 = default(T2);                                                                   <br/>
        ///     T3 dummy_para3;                                                                                 <br/>
        ///     MethodInfo currentMethod;                                                                       <br/>
        ///     currentMethod = MethodAccess(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4)); <br/>
        ///                                                                                                     <br/>
        ///     InvokeTarget = () =>                                                                            <br/>
        ///     {                                                                                               <br/>
        ///         System.Int32 cb_a = (System.Int32)args[0];                                                  <br/>
        ///         System.String cb_b;                                                                         <br/>
        ///         TT cb_c = (TT)args[2];                                                                      <br/>
        ///         System.Object result;                                                                       <br/>
        ///         result = this.Target.Foo[TT](cb_a, out cb_b, ref cb_c);                                     <br/>
        ///                                                                                                     <br/>
        ///         args[1] = (System.Object)cb_b;                                                              <br/>
        ///         args[2] = (System.Object)cb_c;                                                              <br/>
        ///         return result;                                                                              <br/>
        ///     };                                                                                              <br/>         
        ///                                                                                                     <br/>
        ///     System.Object result = Invoke(currentMethod, args, currentMethod);                              <br/>
        ///                                                                                                     <br/>
        ///     para2 = (T2) args[1];                                                                           <br/>
        ///     para3 = (T3) args[2];                                                                           <br/>
        ///                                                                                                     <br/>
        ///     return (TResult) result;                                                                        <br/>
        /// }
        /// </summary>
        public sealed class MethodInterceptorFactory 
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
            /// currentMethod = MethodAccess(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4));
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
                    paramz.Where(param => new[] { ParameterKind.InOut, ParameterKind.Out }.Contains(param.GetParameterKind())).Select(param => DeclareLocal
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
                // urrentMethod = MethodAccess(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4));
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
                                METHOD_ACCESS,
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

            public MethodDeclarationSyntax Build() 
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

                return DeclareMethod(Method).WithBody
                (
                    body: Block
                    (
                        statements: List(statements)
                    )
                );
            }
        }

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
            CallInvoke(variableName, arguments.Select(arg => (ExpressionSyntax) ToIdentifierName(arg)).ToArray());

        /// <summary>
        /// return Target.Bar(...);  <br/>
        ///                          <br/>
        /// OR                       <br/>
        ///                          <br/>
        /// {                        <br/>
        ///   Target.Bar(...);       <br/>
        ///   return;                <br/>
        /// }
        /// </summary>
        internal static StatementSyntax CallTargetAndReturn(MethodInfo method)
        {
            InvocationExpressionSyntax invocation = InvokeMethod(
                method, 
                TARGET,
                castTargetTo: null,
                arguments: method
                    .GetParameters()
                    .Select(p => p.Name)
                    .ToArray());

            return method.ReturnType != typeof(void)
                ? (StatementSyntax) ReturnStatement(invocation)
                : Block
                (
                    statements: List(new StatementSyntax[]{ExpressionStatement(invocation), ReturnStatement()})
                );
        }

        /// <summary>
        /// return Target.Prop;
        /// </summary>
        internal static StatementSyntax ReadTargetAndReturn(PropertyInfo property) =>
            ReturnStatement(PropertyAccess(property, TARGET));

        /// <summary>
        /// Target.Prop = value;
        /// </summary>
        internal static StatementSyntax WriteTarget(PropertyInfo property) =>
            ExpressionStatement
            (
                expression: AssignmentExpression
                (
                    kind: SyntaxKind.SimpleAssignmentExpression,
                    left: PropertyAccess(property, TARGET),
                    right: IdentifierName(Value)
                )
            );

        /// <summary>
        /// if (result == CALL_TARGET) <br/>
        /// {                          <br/>
        ///   ...                      <br/>
        /// }
        /// </summary>
        internal static IfStatementSyntax ShouldCallTarget(LocalDeclarationStatementSyntax result, StatementSyntax ifTrue) =>
            IfStatement
            (
                condition: BinaryExpression
                (
                    kind: SyntaxKind.EqualsExpression, 
                    left: ToIdentifierName(result), 
                    right: CALL_TARGET
                ),
                statement: ifTrue
            );

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
        /// private static readonly PropertyInfo FProp = Properties["IInterface.Prop"]; <br/>
        ///                                                                             <br/>
        /// TResult IInterface.Prop                                                     <br/>
        /// {                                                                           <br/>
        ///     get                                                                     <br/>
        ///     {                                                                       <br/>
        ///         InvokeTarget = () => Target.Prop;                                   <br/>
        ///         return (TResult) Invoke(FProp.GetMethod, new object[0], FProp);     <br/>
        ///     }                                                                       <br/>
        ///     set                                                                     <br/>
        ///     {                                                                       <br/>
        ///         InvokeTarget = () =>                                                <br/>
        ///         {                                                                   <br/>
        ///           Target.Prop = value;                                              <br/>
        ///           return null;                                                      <br/>
        ///         };                                                                  <br/>
        ///         Invoke(FProp.SetMethod, new object[]{ value }, FProp);              <br/>
        ///     }                                                                       <br/>
        /// }
        /// </summary>
        public class PropertyInterceptorFactory 
        {
            public PropertyInfo Property { get; }

            public IdentifierNameSyntax RelatedField { get; }

            public ProxySyntaxFactory<TInterface, TInterceptor> Owner { get; }

            public PropertyInterceptorFactory(PropertyInfo property, ProxySyntaxFactory<TInterface, TInterceptor> owner) 
            {
                Property = property;
                RelatedField = GenerateFieldName(property);
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
            /// return (TResult) this.Invoke(FProp.GetMethod, new object[0], FProp);
            /// </summary>
            internal StatementSyntax CallInvokeAndReturn(params ParameterSyntax[] paramz) => ReturnResult
            (
                Property.PropertyType,
                InvokeMethod
                (
                    INVOKE,
                    target: null,
                    castTargetTo: null,
                    arguments: new ExpressionSyntax[]
                    {
                        Owner.StaticMemberAccess(RelatedField, nameof(PropertyInfo.GetMethod)), // FProp.GetMethod,
                        CreateArray<object>(paramz // new object[0] | new object[] {p1, p2}
                            .Select(param => IdentifierName(param.Identifier))
                            .Cast<ExpressionSyntax>()
                            .ToArray()),
                        Owner.StaticMemberName(RelatedField) // FProp
                    }.Select(Argument).ToArray()
                )
            );

            /// <summary>
            /// this.Invoke(FProp.SetMethod, new object[]{ value }, FProp);
            /// </summary>
            internal StatementSyntax CallInvoke(params ParameterSyntax[] paramz) => ExpressionStatement
            (
                InvokeMethod
                (
                    INVOKE,
                    target: null,
                    castTargetTo: null,
                    arguments: new ExpressionSyntax[]
                    {
                        Owner.StaticMemberAccess(RelatedField, nameof(PropertyInfo.SetMethod)), // FProp.SetMethod
                        CreateArray<object>(paramz //  new object[] {value} | new object[] {p1, p2, value}
                            .Select(param => IdentifierName(param.Identifier))
                            .Append(IdentifierName(Value))
                            .Cast<ExpressionSyntax>()
                            .ToArray()),
                        Owner.StaticMemberName(RelatedField) // FProp
                    }.Select(Argument).ToArray()
                )
            );

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
                    AssignCallback(BuildPropertyGetter()),
                    CallInvokeAndReturn()
                ),
                setBody: Block
                (
                    AssignCallback(BuildPropertySetter()),
                    CallInvoke()
                )
            );

            public IEnumerable<MemberDeclarationSyntax> Build() 
            {
                yield return DeclareField<PropertyInfo>(RelatedField, PROPERTIES, Property);

                yield return DeclareProperty();
            }
        }


        /// <summary>
        /// private static readonly PropertyInfo FItem = Properties["IInterface.Item"];             <br/>
        ///                                                                                         <br/>
        /// TResult IInterface.this[TParam1 p1, TPAram2 p2]                                         <br/>
        /// {                                                                                       <br/>
        ///     get                                                                                 <br/>
        ///     {                                                                                   <br/>
        ///         InvokeTarget = () => Target.Prop[p1, p2];                                       <br/>
        ///         return (TResult) Invoke(FItem.GetMethod, new System.Object[]{p1, p2}, FItem);   <br/>
        ///     }                                                                                   <br/>
        ///     set                                                                                 <br/>
        ///     {                                                                                   <br/>
        ///         InvokeTarget = () =>                                                            <br/>
        ///         {                                                                               <br/>
        ///           Target.Prop[p1, p2] = value;                                                  <br/>
        ///           return null;                                                                  <br/>
        ///         };                                                                              <br/>
        ///         Invoke(FItem.SetMethod, new System.Object[]{ p1, p2, value }, FItem);           <br/>
        ///     }                                                                                   <br/>
        /// }
        /// </summary>
        public sealed class IndexedPropertyInterceptorFactory : PropertyInterceptorFactory
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
                    AssignCallback(BuildPropertyGetter()),
                    CallInvokeAndReturn(paramz.ToArray())
                ),
                setBody: paramz => Block
                (
                    AssignCallback(BuildPropertySetter()),
                    CallInvoke(paramz.ToArray())
                )
            );
        }

        /// <summary>
        /// private static readonly EventInfo FEvent = Events["IInterface.Event"];                     <br/>
        ///                                                                                            <br/>
        /// event EventType IInterface.Event                                                           <br/>
        /// {                                                                                          <br/>
        ///     add                                                                                    <br/>
        ///     {                                                                                      <br/>
        ///         object result = Invoke(FEvent.AddMethod, new object[]{ value }, FEvent);           <br/>
        ///         if (result == CALL_TARGET) Target.Event += value;                                  <br/>
        ///     }                                                                                      <br/>
        ///     remove                                                                                 <br/>
        ///     {                                                                                      <br/>
        ///         object result = Invoke(FEvent.RemoveMethod, new object[]{ value }, FEvent);        <br/>
        ///         if (result == CALL_TARGET) Target.Event -= value;                                  <br/>
        ///     }                                                                                      <br/>
        /// }
        /// </summary>
        internal IEnumerable<MemberDeclarationSyntax> GenerateProxyEvent(EventInfo ifaceEvent)
        {
            IdentifierNameSyntax fieldName = GenerateFieldName(ifaceEvent);

            yield return DeclareField<EventInfo>(fieldName, EVENTS, ifaceEvent);

            yield return DeclareEvent
            (
                @event: ifaceEvent,
                addBody: Block(AddBody()),
                removeBody: Block(RemoveBody())
            );

            IEnumerable<StatementSyntax> AddBody() 
            {
                LocalDeclarationStatementSyntax result = CallInvoke
                (
                    nameof(result),
                    StaticMemberAccess(fieldName, nameof(EventInfo.AddMethod)), // FEvent.AddMethod,
                    CreateArray<object>(IdentifierName(Value)), // new object[] {value}
                    StaticMemberName(fieldName) //FEvent
                );

                yield return result;
                yield return ShouldCallTarget(result, 
                    ifTrue: ExpressionStatement(expression: RegisterEvent(ifaceEvent, TARGET, add: true)));
            }

            IEnumerable<StatementSyntax> RemoveBody() 
            {
                LocalDeclarationStatementSyntax result = CallInvoke
                (
                    nameof(result),
                    StaticMemberAccess(fieldName, nameof(EventInfo.RemoveMethod)), // FEvent.RemoveMethod
                    CreateArray<object>(IdentifierName(Value)),
                    StaticMemberName(fieldName)
                );

                yield return result;
                yield return ShouldCallTarget(result, 
                    ifTrue: ExpressionStatement(expression: RegisterEvent(ifaceEvent, TARGET, add: false)));
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
                    CreateList(new[] { interceptorType, interfaceType}, t => (BaseTypeSyntax) SimpleBaseType(CreateType(t)))
                )
            );

            //
            // Az interceptor altal mar implementalt interface-ek ne szerepeljenek a proxy deklaracioban.
            //

            HashSet<Type> implementedInterfaces = new HashSet<Type>(interceptorType.GetInterfaces());

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>(interceptorType.GetPublicConstructors().Select(DeclareCtor));

            members.AddRange
            (
                interfaceType
                    .ListMembers<MethodInfo>()
                    .Where(m => !implementedInterfaces.Contains(m.DeclaringType) && !m.IsSpecialName)
                    .Select(m => new MethodInterceptorFactory(m).Build())
            );

           /* members.AddRange
            (
                interfaceType
                    .ListMembers<PropertyInfo>()
                    .Where(p => !implementedInterfaces.Contains(p.DeclaringType))
                    .SelectMany(GenerateProxyProperty)
            );
            */
            members.AddRange
            (
                interfaceType
                    .ListMembers<EventInfo>()
                    .Where(e => !implementedInterfaces.Contains(e.DeclaringType))
                    .SelectMany(GenerateProxyEvent)
            );

            return cls.WithMembers(List(members));
        }
    }
}