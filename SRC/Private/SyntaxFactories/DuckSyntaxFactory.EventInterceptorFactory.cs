/********************************************************************************
* DuckSyntaxFactory.EventInterceptorFactory.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory<TInterface, TTarget>
    {
        /// <summary>
        /// event TDelegate IFoo[System.Int32].Event     <br/>
        /// {                                            <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   add => Target.Event += value;              <br/>
        ///   [MethodImplAttribute(AggressiveInlining)]  <br/>
        ///   remove => Target.Event -= value;           <br/>
        /// }
        /// </summary>
        internal sealed class EventInterceptorFactory : InterceptorFactoryBase
        {
            public EventInterceptorFactory(DuckSyntaxFactory<TInterface, TTarget> owner) : base(owner) { }

            public override MemberDeclarationSyntax Build(MemberInfo member)
            {
                EventInfo
                    ifaceEvt = (EventInfo) member,
                    targetEvt = GetTargetMember(ifaceEvt);

                //
                // Ellenorizzuk h az esemeny lathato e a legeneralando szerelvenyunk szamara.
                //

                Visibility.Check(targetEvt, Owner.AssemblyName, checkAdd: ifaceEvt.AddMethod != null, checkRemove: ifaceEvt.RemoveMethod != null);

                MethodInfo accessor = (ifaceEvt.AddMethod ?? ifaceEvt.RemoveMethod)!;
                Debug.Assert(accessor != null);

                Type? castTargetTo = accessor!.GetAccessModifiers() == AccessModifiers.Explicit ? accessor!.GetDeclaringType() : null;

                return DeclareEvent
                (
                    ifaceEvt,
                    addBody: ArrowExpressionClause
                    (
                        expression: RegisterEvent(targetEvt, TARGET, add: true, IdentifierName(Value), castTargetTo)
                    ),
                    removeBody: ArrowExpressionClause
                    (
                        expression: RegisterEvent(targetEvt, TARGET, add: false, IdentifierName(Value), castTargetTo)
                    ),
                    forceInlining: true
                );
            }

            public override bool IsCompatible(MemberInfo member) => member is EventInfo evt && evt.DeclaringType.IsInterface;

            protected override bool SignatureEquals(MemberInfo targetMember, MemberInfo ifaceMember)
            {
                EventInfo
                    targetEvt = (EventInfo) targetMember,
                    ifaceEvt = (EventInfo) ifaceMember;

                return targetEvt.EventHandlerType == ifaceEvt.EventHandlerType;
            }
        }
    }
}
