/********************************************************************************
* DuckSyntaxFactory.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal class DuckSyntaxFactory<TInterface, TTarget> : ProxySyntaxFactoryBase
    {
        private static readonly MemberAccessExpressionSyntax
            //
            // this.Target
            //
            TARGET = MemberAccess(null, MemberInfoExtensions.ExtractFrom<DuckBase<TTarget>>(ii => ii.Target));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowIfNotFound(MemberInfo targetMember, MemberInfo ifaceMember)
        {
            if (targetMember == null) 
                throw new MissingMemberException(string.Format(Resources.Culture, Resources.MISSING_IMPLEMENTATION, ifaceMember.GetFullName()));
        }

        /// <summary>
        /// [MethodImplAttribute(AggressiveInlining)]  <br/>
        /// ref TResult IFoo[TGeneric1].Bar[TGeneric2](TGeneric1 para1, ref T1 para2, out T2 para3, TGeneric2 para4) => ref Target.Foo[TGeneric2](para1, ref para2, out para3, para4);
        /// </summary>
        internal MethodDeclarationSyntax GenerateDuckMethod(MethodInfo ifaceMethod)
        {
            MethodInfo targetMethod = typeof(TTarget)
                .ListMembers(System.Reflection.TypeExtensions.GetMethods, includeNonPublic: true)
                .SingleOrDefault(m => 
                    m.Name.Equals(ifaceMethod.Name, StringComparison.Ordinal) &&

                    //
                    // Azert nem a "GetMethod(string, Type[])"-ot hasznaljuk mert az nem fogja megtalalni 
                    // a nyilt generikus metodusokat mivel pl.:
                    //
                    // "interface IFoo {void Foo<T>(T para);}" es "class Foo {void Foo<T>(T para){}}"
                    //
                    // eseten amennyiben Foo nem valositja meg IFoo-t a ket generikus "T" nem ugyanaz a tipus.
                    //

                    ArgumentComparer.Instance.Equals(m.ReturnType, ifaceMethod.ReturnType)                              &&
                    m.GetGenericArguments().SequenceEqual(ifaceMethod.GetGenericArguments(), ArgumentComparer.Instance) &&  
                    m.GetParameters().SequenceEqual(ifaceMethod.GetParameters(), ParameterComparer.Instance));

            ThrowIfNotFound(targetMethod, ifaceMethod);

            //
            // Ellenorizzuk h a metodus lathato e a legeneralando szerelvenyunk szamara.
            //

            Visibility.Check(targetMethod, AssemblyName);

            ExpressionSyntax invocation = InvokeMethod
            (
                targetMethod, 
                TARGET, 
                ifaceMethod
                    .GetParameters()
                    .Select(para => para.Name)
                    .ToArray()
            );

            if (ifaceMethod.ReturnType.IsByRef) invocation = RefExpression(invocation);

            return DeclareMethod(ifaceMethod, forceInlining: true)
                .WithExpressionBody
                (
                    expressionBody: ArrowExpressionClause(invocation)
                )
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        /// <summary>
        /// System.Int32 IFoo[System.Int32].Prop         <br/>
        /// {                                            <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   get => Target.Prop;                        <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   set => Target.Prop = value;                <br/>
        /// }
        /// </summary>
        internal MemberDeclarationSyntax GenerateDuckProperty(PropertyInfo ifaceProperty)
        {
            PropertyInfo targetProperty = typeof(TTarget)
                .ListMembers(System.Reflection.TypeExtensions.GetProperties, includeNonPublic: true)
                .SingleOrDefault(p => 
                    p.Name == ifaceProperty.Name && 
                    p.PropertyType == ifaceProperty.PropertyType &&

                    //
                    // Ha az interface tulajdonsaga irhato akkor targetnak is irhatonak kell lennie
                    // (kulomben mind1 h irhato e v sem).
                    //

                    (!ifaceProperty.CanWrite || p.CanWrite) &&
                    (!ifaceProperty.CanRead || p.CanRead) &&

                    //
                    // Indexer property-knel pedig meg kell egyezniuk az index parameterek
                    // sorrendjenek es tipusanak.
                    //

                    ifaceProperty
                        .GetIndexParameters()
                        .Select(ip => ip.ParameterType)
                        .SequenceEqual
                        (
                            p.GetIndexParameters().Select(ip => ip.ParameterType)
                        ));

            ThrowIfNotFound(targetProperty, ifaceProperty);

            //
            // Ellenorizzuk h a property lathato e a legeneralando szerelvenyunk szamara.
            //

            Visibility.Check(targetProperty, AssemblyName, checkGet: ifaceProperty.CanRead, checkSet: ifaceProperty.CanWrite);

            //
            // Ne a "targetProperty"-n hivjuk h akkor is jol mukodjunk ha az interface indexerenek
            // maskepp vannak elnvezve a parameterei.
            //

            ExpressionSyntax propertyAccess = PropertyAccess(ifaceProperty, TARGET);

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            ArrowExpressionClauseSyntax 
                getBody = ArrowExpressionClause
                (
                    expression: propertyAccess
                ),
                setBody = ArrowExpressionClause
                (
                    expression: AssignmentExpression
                    (
                        kind: SyntaxKind.SimpleAssignmentExpression,
                        left: propertyAccess,
                        right: IdentifierName(Value)
                    )
                );

            return ifaceProperty.IsIndexer()
                ? DeclareIndexer
                (
                    property: ifaceProperty,
                    getBody: paramz => getBody,
                    setBody: paramz => setBody,
                    forceInlining: true
                )
                : (MemberDeclarationSyntax) DeclareProperty
                (
                    property: ifaceProperty,
                    getBody: getBody,
                    setBody: setBody,
                    forceInlining: true
                );
        }

        /// <summary>
        /// event TDelegate IFoo[System.Int32].Event     <br/>
        /// {                                            <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   add => Target.Event += value;              <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   remove => Target.Event -= value;           <br/>
        /// }
        /// </summary>
        internal EventDeclarationSyntax GenerateDuckEvent(EventInfo ifaceEvent)
        {
            EventInfo targetEvent = typeof(TTarget)
                .ListMembers(System.Reflection.TypeExtensions.GetEvents, includeNonPublic: true)
                .SingleOrDefault(ev => ev.Name == ifaceEvent.Name && ev.EventHandlerType == ifaceEvent.EventHandlerType);

            ThrowIfNotFound(targetEvent, ifaceEvent);

            //
            // Ellenorizzuk h az esemeny lathato e a legeneralando szerelvenyunk szamara.
            //

            Visibility.Check(targetEvent, AssemblyName, checkAdd: ifaceEvent.AddMethod != null, checkRemove: ifaceEvent.RemoveMethod != null);

            return DeclareEvent
            (
                ifaceEvent, 
                addBody: ArrowExpressionClause
                (
                    expression: RegisterEvent(targetEvent, TARGET, add: true)
                ),
                removeBody: ArrowExpressionClause
                (
                    expression: RegisterEvent(targetEvent, TARGET, add: false)
                ),
                forceInlining: true
            );
        }

        protected internal override ClassDeclarationSyntax GenerateProxyClass()
        {
            Type 
                interfaceType = typeof(TInterface),
                @base = typeof(DuckBase<TTarget>);

            Debug.Assert(interfaceType.IsInterface());
            Debug.Assert(!interfaceType.IsGenericTypeDefinition());
            Debug.Assert(!@base.IsGenericTypeDefinition());

            ClassDeclarationSyntax cls = ClassDeclaration
            (
                identifier: GeneratedClassName
            )
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
                    CreateList(new[] { @base, interfaceType }, t => (BaseTypeSyntax) SimpleBaseType(CreateType(t)))
                )
            );

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[]
            {
                DeclareCtor(@base.GetApplicableConstructor())
            });

            var exceptions = new List<Exception>();
            
            members.AddRange
            (
                interfaceType
                    .ListMembers(System.Reflection.TypeExtensions.GetMethods)
                    .Where(m => !m.IsSpecialName)
                    .Select(m => AggregateException(m, GenerateDuckMethod))
            );  
            
            members.AddRange
            (
                interfaceType
                    .ListMembers(System.Reflection.TypeExtensions.GetProperties)
                    .Select(p => AggregateException(p, GenerateDuckProperty))
            );

            members.AddRange
            (
                interfaceType
                    .ListMembers(System.Reflection.TypeExtensions.GetEvents)
                    .Select(e => AggregateException(e, GenerateDuckEvent))
            );

            //
            // Az osszes (generator altal dobott) hibat visszaadjuk (ha voltak).
            //

            if (exceptions.Any()) throw exceptions.Count == 1 ? exceptions.Single() : new AggregateException(exceptions);

            return cls.WithMembers(List(members));

            TResult AggregateException<T, TResult>(T arg, Func<T, TResult> selector)
            {
                try
                {
                    return selector(arg);
                }
                catch (Exception e)
                {
                    if (e is MissingMemberException || e is MemberAccessException)
                    {

                        exceptions.Add(e);
                        return default(TResult);
                    }
                    throw;
                }
            }
        }

        public override string AssemblyName => $"{CreateType<TTarget>()}_{CreateType<TInterface>()}_Duck";
    }
}
