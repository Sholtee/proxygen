/********************************************************************************
* DuckSyntaxFactory.EventInterceptorFactory.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
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
            public override MemberDeclarationSyntax Build(IMemberInfo member)
            {
                IEventInfo
                    ifaceEvt  = (IEventInfo) member,
                    targetEvt = GetTargetMember(ifaceEvt, MetadataTypeInfo.CreateFrom(typeof(TTarget)).Events);

                //
                // Ellenorizzuk h az esemeny lathato e a legeneralando szerelvenyunk szamara.
                //

                Visibility.Check(targetEvt, AssemblyName, checkAdd: ifaceEvt.AddMethod != null, checkRemove: ifaceEvt.RemoveMethod != null);

                IMethodInfo accessor = ifaceEvt.AddMethod ?? ifaceEvt.RemoveMethod!;

                ITypeInfo? castTargetTo = accessor.AccessModifiers == AccessModifiers.Explicit ? accessor.DeclaringType : null;

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

            public override bool IsCompatible(IMemberInfo member) => base.IsCompatible(member) && member is IEventInfo;

            protected override bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember)
            {
                IEventInfo
                    targetEvt = (IEventInfo) targetMember,
                    ifaceEvt  = (IEventInfo) ifaceMember;

                return targetEvt.Type.Equals(ifaceEvt.Type);
            }
        }
    }
}
