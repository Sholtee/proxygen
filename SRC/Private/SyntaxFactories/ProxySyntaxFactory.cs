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
            TARGET = MemberAccess(null, MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.Target)),
            //
            // this.CALL_TARGET
            //
            CALL_TARGET = MemberAccess(null, MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.CALL_TARGET));

        private static readonly MethodInfo
            INVOKE = (MethodInfo) MemberInfoExtensions.ExtractFrom<InterfaceInterceptor<TInterface>>(ii => ii.Invoke(default, default, default)),
            METHOD_ACCESS = (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.MethodAccess(default));

        private static readonly FieldInfo
            EVENTS = (FieldInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.Events),
            PROPERTIES = (FieldInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.Properties);

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
        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)  <br/>
        /// {                                                                                       <br/>
        ///   ...                                                                                   <br/>
        ///   object[] args = new object[]{para1, para2, default(T3), para4};                       <br/>
        ///   ...                                                                                   <br/>
        /// }
        /// </summary>
        internal static LocalDeclarationStatementSyntax CreateArgumentsArray(MethodInfo method) => DeclareLocal<object[]>("args", CreateArray<object>(method
            .GetParameters()
            .Select(param => param.IsOut ? DefaultExpression(CreateType(param.ParameterType)) : (ExpressionSyntax) IdentifierName(param.Name))
            .ToArray()));

        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)   <br/>
        /// {                                                                                        <br/>
        ///   ...                                                                                    <br/>
        ///   para2 = (T2) args[1];                                                                  <br/>
        ///   para3 = (T3) args[2];                                                                  <br/>
        ///   ...                                                                                    <br/>
        /// }
        /// </summary>
        internal static IEnumerable<ExpressionStatementSyntax> AssignByRefParameters(MethodInfo method, LocalDeclarationStatementSyntax argsArray)
        {
            IdentifierNameSyntax array = ToIdentifierName(argsArray);

            return method
                .GetParameters()
                .Select((param, i) => new {Parameter = param, Index = i})
                .Where(p => p.Parameter.ParameterType.IsByRef && !p.Parameter.IsIn)
                .Select
                (
                    p => ExpressionStatement
                    (
                        expression: AssignmentExpression
                        (
                            kind:  SyntaxKind.SimpleAssignmentExpression, 
                            left:  IdentifierName(p.Parameter.Name),
                            right: CastExpression
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
                    )
                );
        }

        /// <summary>
        /// T2 dummy_para2 = default(T2);                                                                                <br/>
        /// T3 dummy_para3;                                                                                              <br/>
        /// MethodInfo currentMethod = MethodAccess(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4));
        /// </summary>
        internal static IEnumerable<LocalDeclarationStatementSyntax> AcquireMethodInfo(MethodInfo method, out LocalDeclarationStatementSyntax currentMethod)
        {
            IReadOnlyList<ParameterInfo> paramz = method.GetParameters();

            var statements = new List<LocalDeclarationStatementSyntax>();

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
            // MethodInfo currentMethod = MethodAccess(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4));
            //

            currentMethod = DeclareLocal<MethodInfo>(nameof(currentMethod), InvokeMethod
            (
                METHOD_ACCESS,
                target: null,
                castTarget: false,
                Argument
                (
                    expression: ParenthesizedLambdaExpression
                    (
                        parameterList: ParameterList(), // Roslyn 3.4.0 felrobban ha nincs parameter lista (3.3.X-nel meg opcionalis volt)
                        body: InvokeMethod
                        (
                            method, 
                            TARGET,
                            castTarget: false,

                            //
                            // GetDummyName() azert kell mert ByRef parameterek nem szerepelhetnek kifejezesekben.
                            //

                            arguments: paramz.Select(param => param.ParameterType.IsByRef ? GetDummyName(param) : param.Name).ToArray()
                        )
                    )
                )
            ));

            statements.Add(currentMethod);

            return statements;

            string GetDummyName(ParameterInfo param) => $"dummy_{param.Name}";
        }

        /// <summary>
        /// object result = Invoke(...);
        /// </summary>
        internal static LocalDeclarationStatementSyntax CallInvoke(params ExpressionSyntax[] arguments) =>
            DeclareLocal<object>("result", InvokeMethod
            (
                INVOKE,
                target: null,
                castTarget: false,
                arguments: arguments.Select(Argument).ToArray()
            ));

        /// <summary>
        /// object result = Invoke(var1, var2, ..., varN);
        /// </summary>
        internal static LocalDeclarationStatementSyntax CallInvoke(params LocalDeclarationStatementSyntax[] arguments) =>
            CallInvoke(arguments.Select(arg => (ExpressionSyntax) ToIdentifierName(arg)).ToArray());

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
                castTarget: false,
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
        internal static ReturnStatementSyntax ReturnResult(Type returnType, ExpressionSyntax result) => ReturnStatement
        (
            expression: returnType == typeof(void)
                ? null
                : CastExpression
                (
                    type: CreateType(returnType),
                    expression: result
                )
        );

        /// <summary>
        /// return;             <br/>
        ///                     <br/>
        /// OR                  <br/>
        ///                     <br/>
        /// return (T) result;
        /// </summary>
        internal static ReturnStatementSyntax ReturnResult(Type returnType, LocalDeclarationStatementSyntax result) =>
            ReturnResult(returnType, ToIdentifierName(result));

        /// <summary>
        /// TResult IInterface.Foo[TGeneric](T1 para1, ref T2 para2, out T3 para3, TGeneric para4)                         <br/>
        /// {                                                                                                              <br/>
        ///     object[] args = new object[] {para1, para2, default(T3), para4};                                           <br/>
        ///                                                                                                                <br/>
        ///     T2 dummy_para2 = default(T2);                                                                              <br/>
        ///     T3 dummy_para3;                                                                                            <br/>
        ///     MethodInfo currentMethod = MethodAccess(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4)); <br/>
        ///                                                                                                                <br/>
        ///     object result = Invoke(currentMethod, args, currentMethod);                                                <br/>
        ///     if (result == CALL_TARGET) return Target.Foo(para1, ref para2, out para3, para4);                          <br/>
        ///                                                                                                                <br/>
        ///     para2 = (T2) args[1];                                                                                      <br/>
        ///     para3 = (T3) args[2];                                                                                      <br/>
        ///                                                                                                                <br/>
        ///     return (TResult) result;                                                                                   <br/>
        /// }
        /// </summary>
        internal static MethodDeclarationSyntax GenerateProxyMethod(MethodInfo ifaceMethod)
        {
            //
            // "ref" visszateres nem tamogatott.
            //

            Type returnType = ifaceMethod.ReturnType;
            if (returnType.IsByRef)
                throw new NotSupportedException(Resources.REF_RETURNS_NOT_SUPPORTED);

            var statements = new List<StatementSyntax>();

            LocalDeclarationStatementSyntax currentMethod;
            statements.AddRange(AcquireMethodInfo(ifaceMethod, out currentMethod));

            LocalDeclarationStatementSyntax args = CreateArgumentsArray(ifaceMethod);
            statements.Add(args);

            LocalDeclarationStatementSyntax result = CallInvoke(currentMethod, args, currentMethod);
            statements.Add(result);

            statements.Add(ShouldCallTarget(result, 
                ifTrue: CallTargetAndReturn(ifaceMethod)));

            statements.AddRange(AssignByRefParameters(ifaceMethod, args));

            if (returnType != typeof(void)) statements.Add
            (
                ReturnResult(returnType, result)
            );

            return DeclareMethod(ifaceMethod).WithBody
            (
                body: Block
                (
                    statements: List(statements)
                )
            );
        }

        /// <summary>
        /// private static readonly PropertyInfo FProp = Properties["IInterface.Prop"];    <br/>
        ///                                                                                <br/>
        /// TResult IInterface.Prop                                                        <br/>
        /// {                                                                              <br/>
        ///     get                                                                        <br/>
        ///     {                                                                          <br/>
        ///         object result = Invoke(FProp.GetMethod, new object[0], FProp);         <br/>
        ///         if (result == CALL_TARGET) return Target.Prop;                         <br/>
        ///                                                                                <br/>
        ///         return (TResult) result;                                               <br/>
        ///     }                                                                          <br/>
        ///     set                                                                        <br/>
        ///     {                                                                          <br/>
        ///         object result = Invoke(FProp.SetMethod, new object[]{ value }, FProp); <br/>
        ///         if (result == CALL_TARGET) Target.Prop = value;                        <br/>
        ///     }                                                                          <br/>
        /// }
        /// </summary>
        internal IEnumerable<MemberDeclarationSyntax> GenerateProxyProperty(PropertyInfo ifaceProperty)
        {
            IdentifierNameSyntax fieldName = GenerateFieldName(ifaceProperty);

            yield return DeclareField<PropertyInfo>(fieldName, PROPERTIES, ifaceProperty);

            if (ifaceProperty.IsIndexer())
            {
                yield return GenerateProxyIndexer(ifaceProperty, fieldName);
                yield break;
            }

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            yield return DeclareProperty
            (
                property: ifaceProperty,
                getBody: Block(GetBody()),
                setBody: Block(SetBody())
            );

            IEnumerable<StatementSyntax> GetBody() 
            {
                LocalDeclarationStatementSyntax result = CallInvoke
                (
                    StaticMemberAccess(fieldName, nameof(PropertyInfo.GetMethod)), // FProp.GetMethod,
                    CreateArray<object>(), // new object[0]
                    StaticMemberName(fieldName) // FProp
                );

                yield return result;
                yield return ShouldCallTarget(result, 
                    ifTrue: ReadTargetAndReturn(ifaceProperty));
                yield return ReturnResult(ifaceProperty.PropertyType, result);
            }

            IEnumerable<StatementSyntax> SetBody() 
            {
                LocalDeclarationStatementSyntax result = CallInvoke
                (
                    StaticMemberAccess(fieldName, nameof(PropertyInfo.SetMethod)), // FProp.SetMethod
                    CreateArray<object>(IdentifierName(Value)), // new object[] {value}
                    StaticMemberName(fieldName) // FProp
                );

                yield return result;
                yield return ShouldCallTarget(result, 
                    ifTrue: WriteTarget(ifaceProperty));
            }
        }

        /// <summary>
        /// TResult IInterface.this[TParam1 p1, TPAram2 p2]                                                 <br/>
        /// {                                                                                               <br/>
        ///     get                                                                                         <br/>
        ///     {                                                                                           <br/>
        ///         object result = Invoke(FProp.GetMethod, new object[]{ p1, p2 }, FProp);                 <br/>
        ///         if (result == CALL_TARGET) return Target[p1, p2];                                       <br/>
        ///                                                                                                 <br/>
        ///         return (TResult) result;                                                                <br/>
        ///     }                                                                                           <br/>
        ///     set                                                                                         <br/>
        ///     {                                                                                           <br/>
        ///         object result = Invoke(FProp.SetMethod, new object[]{ p1, p2, value }, FProp);          <br/>
        ///         if (result == CALL_TARGET) Target[p1, p2] = value;                                      <br/>
        ///     }                                                                                           <br/>
        /// }
        /// </summary>
        internal MemberDeclarationSyntax GenerateProxyIndexer(PropertyInfo ifaceProperty, IdentifierNameSyntax fieldName)
        {
            return DeclareIndexer
            (
                property: ifaceProperty,
                getBody: paramz => Block(GetBody(paramz)),
                setBody: paramz => Block(SetBody(paramz))
            );

            IEnumerable<StatementSyntax> GetBody(IEnumerable<ParameterSyntax> paramz) 
            {
                LocalDeclarationStatementSyntax result = CallInvoke
                (
                    StaticMemberAccess(fieldName, nameof(PropertyInfo.GetMethod)), // FProp.GetMethod,,
                    CreateArray<object>(paramz // new object[] {p1, p2}
                        .Select(param => IdentifierName(param.Identifier))
                        .Cast<ExpressionSyntax>()
                        .ToArray()),
                    StaticMemberName(fieldName) // FProp
                );

                yield return result;
                yield return ShouldCallTarget(result, 
                    ifTrue: ReadTargetAndReturn(ifaceProperty));
                yield return ReturnResult(ifaceProperty.PropertyType, result);
            }

            IEnumerable<StatementSyntax> SetBody(IEnumerable<ParameterSyntax> paramz) 
            {
                LocalDeclarationStatementSyntax result = CallInvoke
                (
                    StaticMemberAccess(fieldName, nameof(PropertyInfo.SetMethod)), // FProp.SetMethod,
                    CreateArray<object>(paramz // new object[] {p1, p2, value}
                        .Select(param => IdentifierName(param.Identifier))
                        .Append(IdentifierName(Value))
                        .Cast<ExpressionSyntax>()
                        .ToArray()),
                    StaticMemberName(fieldName) // FProp
                );

                yield return result;
                yield return ShouldCallTarget(result, 
                    ifTrue: WriteTarget(ifaceProperty));
            }
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

        public override string AssemblyName => $"{CreateType<TInterceptor>()}_{CreateType<TInterface>()}_Proxy"; 

        protected internal override ClassDeclarationSyntax GenerateProxyClass()
        {
            Type
                interfaceType   = typeof(TInterface),
                interceptorType = typeof(TInterceptor);

            Debug.Assert(interfaceType.IsInterface());

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

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[]
            {
                DeclareCtor(interceptorType.GetApplicableConstructor(AssemblyName))
            });

            members.AddRange
            (
                interfaceType
                    .ListMembers<MethodInfo>()
                    .Where(m => !implementedInterfaces.Contains(m.DeclaringType) && !m.IsSpecialName)
                    .Select(GenerateProxyMethod)
            );

            members.AddRange
            (
                interfaceType
                    .ListMembers<PropertyInfo>()
                    .Where(p => !implementedInterfaces.Contains(p.DeclaringType))
                    .SelectMany(GenerateProxyProperty)
            );

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