/********************************************************************************
* DuckSyntaxFactory.EventInterceptorFactory.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvents(ClassDeclarationSyntax cls, object context)
        {
            foreach (IEventInfo ifaceEvt in InterfaceType.Events)
            {
                IEventInfo targetEvt = GetTargetMember(ifaceEvt, TargetType.Events, SignatureEquals);

                //
                // Ellenorizzuk h az esemeny lathato e a legeneralando szerelvenyunk szamara.
                //

                Visibility.Check
                (
                    targetEvt,
                    ContainingAssembly,
                    checkAdd: ifaceEvt.AddMethod is not null,
                    checkRemove: ifaceEvt.RemoveMethod is not null
                );

                cls = ResolveEvent(cls, ifaceEvt, targetEvt);
            }

            return cls;

            [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "There is not dead code.")]
            static bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember)
            {
                if (targetMember is not IEventInfo targetEvent || ifaceMember is not IEventInfo ifaceEvent)
                    return false;

                if (ifaceEvent.AddMethod is not null)
                {
                    if (targetEvent.AddMethod?.SignatureEquals(ifaceEvent.AddMethod, ignoreVisibility: true) is not true)
                        return false;
                }

                if (ifaceEvent.RemoveMethod is not null)
                {
                    if (targetEvent.RemoveMethod?.SignatureEquals(ifaceEvent.RemoveMethod, ignoreVisibility: true) is not true)
                        return false;
                }

                return true;
            }
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
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo targetEvt)
        {
            IEventInfo ifaceEVt = (IEventInfo) context;

            IMethodInfo accessor = targetEvt.AddMethod ?? targetEvt.RemoveMethod!;

            ITypeInfo? castTargetTo = accessor.AccessModifiers is AccessModifiers.Explicit
                ? accessor.DeclaringInterfaces.Single() // explicit esemenyhez biztosan csak egy deklaralo interface tartozik
                : null;

            return cls.AddMembers
            (
                ResolveEvent
                (
                    ifaceEVt,
                    addBody: CreateBody(register: true),
                    removeBody: CreateBody(register: false),
                    forceInlining: true
                )
            );

            ArrowExpressionClauseSyntax CreateBody(bool register) => ArrowExpressionClause
            (
                expression: RegisterEvent
                (
                    targetEvt,
                    MemberAccess(null, Target),
                    register,
                    IdentifierName(Value),
                    castTargetTo
                )
            );
        }
    }
}
