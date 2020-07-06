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
            TARGET = MemberAccess(null, MemberInfoExtensions.ExtractFrom<DuckBase<TTarget>>(ii => ii.Target!));

        private static TMember GetTargetMember<TMember>(Type target, TMember ifaceMember) where TMember: MemberInfo
        {
            TMember[] possibleTargets = target
                .ListMembers<TMember>(includeNonPublic: true)
                .Where(member => member.SignatureEquals(ifaceMember))
                .ToArray();

            if (!possibleTargets.Any()) 
                throw new MissingMemberException(string.Format(Resources.Culture, Resources.MISSING_IMPLEMENTATION, ifaceMember.GetFullName()));

            //
            // Lehet tobb implementacio is pl.:
            // "List<T>: ICollection<T>, IList" ahol IList nem ICollection<T> ose es mindkettonek van ReadOnly tulajdonsaga.
            //

            if (possibleTargets.Length > 1)
                throw new AmbiguousMatchException(string.Format(Resources.Culture, Resources.AMBIGUOUS_MATCH, ifaceMember.GetFullName()));

            return possibleTargets[0];
        }

        /// <summary>
        /// [MethodImplAttribute(AggressiveInlining)]  <br/>
        /// ref TResult IFoo[TGeneric1].Bar[TGeneric2](TGeneric1 para1, ref T1 para2, out T2 para3, TGeneric2 para4) => ref Target.Foo[TGeneric2](para1, ref para2, out para3, para4);
        /// </summary>
        internal MethodDeclarationSyntax GenerateDuckMethod(MethodInfo ifaceMethod)
        {
            MethodInfo targetMethod = GetTargetMember(typeof(TTarget), ifaceMethod);

            //
            // Ellenorizzuk h a metodus lathato e a legeneralando szerelvenyunk szamara.
            //

            Visibility.Check(targetMethod, AssemblyName);

            ExpressionSyntax invocation = InvokeMethod
            (
                targetMethod, 
                TARGET, 
                castTargetTo: targetMethod.GetAccessModifiers() == AccessModifiers.Explicit 
                    ? targetMethod.GetDeclaringType() 
                    : null,
                arguments: ifaceMethod
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
            PropertyInfo targetProperty = GetTargetMember(typeof(TTarget), ifaceProperty);

            //
            // Ellenorizzuk h a property lathato e a legeneralando szerelvenyunk szamara.
            //

            Visibility.Check(targetProperty, AssemblyName, checkGet: ifaceProperty.CanRead, checkSet: ifaceProperty.CanWrite);       

            MethodInfo accessor = (targetProperty.GetMethod ?? targetProperty.SetMethod)!;
            Debug.Assert(accessor != null);

            //
            // Ne a "targetProperty"-n hivjuk h akkor is jol mukodjunk ha az interface indexerenek
            // maskepp vannak elnvezve a parameterei.
            //

            ExpressionSyntax propertyAccess = PropertyAccess
            (
                ifaceProperty, 
                TARGET, 
                castTargetTo: accessor!.GetAccessModifiers() == AccessModifiers.Explicit 
                    ? accessor!.GetDeclaringType() 
                    : null
            );

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
            EventInfo targetEvent = GetTargetMember(typeof(TTarget), ifaceEvent);

            //
            // Ellenorizzuk h az esemeny lathato e a legeneralando szerelvenyunk szamara.
            //

            Visibility.Check(targetEvent, AssemblyName, checkAdd: ifaceEvent.AddMethod != null, checkRemove: ifaceEvent.RemoveMethod != null);

            MethodInfo accessor = (ifaceEvent.AddMethod ?? ifaceEvent.RemoveMethod)!;
            Debug.Assert(accessor != null);

            Type? castTargetTo = accessor!.GetAccessModifiers() == AccessModifiers.Explicit ? accessor!.GetDeclaringType() : null;

            return DeclareEvent
            (
                ifaceEvent, 
                addBody: ArrowExpressionClause
                (
                    expression: RegisterEvent(targetEvent, TARGET, add: true, castTargetTo)
                ),
                removeBody: ArrowExpressionClause
                (
                    expression: RegisterEvent(targetEvent, TARGET, add: false, castTargetTo)
                ),
                forceInlining: true
            );
        }

        protected internal override ClassDeclarationSyntax GenerateProxyClass()
        {
            Type 
                interfaceType = typeof(TInterface),
                @base = typeof(DuckBase<TTarget>);

            Debug.Assert(interfaceType.IsInterface);
            Debug.Assert(!interfaceType.IsGenericTypeDefinition);
            Debug.Assert(!@base.IsGenericTypeDefinition);

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

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>(@base.GetPublicConstructors().Select(DeclareCtor));

            var exceptions = new List<Exception>();
            
            members.AddRange
            (
                interfaceType
                    .ListMembers<MethodInfo>()
                    .Where(m => !m.IsSpecialName)
                    .Select(m => AggregateException(m, GenerateDuckMethod))
            );  
            
            members.AddRange
            (
                interfaceType
                    .ListMembers<PropertyInfo>()
                    .Select(p => AggregateException(p, GenerateDuckProperty))
            );

            members.AddRange
            (
                interfaceType
                    .ListMembers<EventInfo>()
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
                    if (e is MissingMemberException || e is MemberAccessException || e is AmbiguousMatchException)
                    {

                        exceptions!.Add(e);
                        return default(TResult)!;
                    }
                    throw;
                }
            }
        }

        public override string AssemblyName => $"{GetSafeTypeName<TTarget>()}_{GetSafeTypeName<TInterface>()}_Duck";
    }
}
