/********************************************************************************
* DuckSyntaxFactory.EventInterceptorFactory.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
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
        internal sealed class EventInterceptorFactory : DuckMemberSyntaxFactory
        {
            protected override IEnumerable<MemberDeclarationSyntax> Build()
            {
                foreach (IEventInfo ifaceEvt in Context.InterfaceType.Events)
                {
                    IEventInfo targetEvt = GetTargetMember(ifaceEvt, Context.TargetType.Events);

                    //
                    // Ellenorizzuk h az esemeny lathato e a legeneralando szerelvenyunk szamara.
                    //

                    Visibility.Check(targetEvt, Context.AssemblyName, checkAdd: ifaceEvt.AddMethod != null, checkRemove: ifaceEvt.RemoveMethod != null);

                    IMethodInfo accessor = ifaceEvt.AddMethod ?? ifaceEvt.RemoveMethod!;

                    ITypeInfo? castTargetTo = accessor.AccessModifiers == AccessModifiers.Explicit ? accessor.DeclaringType : null;

                    yield return DeclareEvent
                    (
                        ifaceEvt,
                        addBody: ArrowExpressionClause
                        (
                            expression: RegisterEvent(targetEvt, MemberAccess(null, TARGET), add: true, IdentifierName(Value), castTargetTo)
                        ),
                        removeBody: ArrowExpressionClause
                        (
                            expression: RegisterEvent(targetEvt, MemberAccess(null, TARGET), add: false, IdentifierName(Value), castTargetTo)
                        ),
                        forceInlining: true
                    );
                }
            }

            protected override bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember)
            {
                IEventInfo
                    targetEvt = (IEventInfo) targetMember,
                    ifaceEvt  = (IEventInfo) ifaceMember;

                return targetEvt.Type.Equals(ifaceEvt.Type) && !targetEvt.IsStatic;
            }

            public EventInterceptorFactory(IDuckContext context) : base(context) { }
        }
    }
}
