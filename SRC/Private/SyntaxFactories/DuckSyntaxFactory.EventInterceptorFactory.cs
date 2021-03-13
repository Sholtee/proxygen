/********************************************************************************
* DuckSyntaxFactory.EventInterceptorFactory.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            protected override IEnumerable<MemberDeclarationSyntax> BuildMembers(CancellationToken cancellation)
            {
                foreach (IEventInfo ifaceEvt in Context.InterfaceType.Events)
                {
                    cancellation.ThrowIfCancellationRequested();

                    IEventInfo targetEvt = GetTargetMember(ifaceEvt, Context.TargetType.Events);

                    //
                    // Ellenorizzuk h az esemeny lathato e a legeneralando szerelvenyunk szamara.
                    //

                    Visibility.Check
                    (
                        targetEvt, 
                        Context.ContainingAssembly, 
                        checkAdd: ifaceEvt.AddMethod is not null, 
                        checkRemove: ifaceEvt.RemoveMethod is not null
                    );

                    IMethodInfo accessor = ifaceEvt.AddMethod ?? ifaceEvt.RemoveMethod!;

                    ITypeInfo? castTargetTo = accessor.AccessModifiers == AccessModifiers.Explicit 
                        ? accessor.DeclaringInterfaces.Single() // explicit esemenyhez biztosan csak egy deklaralo interface tartozik
                        : null;

                    yield return DeclareEvent
                    (
                        ifaceEvt,
                        addBody: CreateBody(register: true),
                        removeBody: CreateBody(register: false),
                        forceInlining: true
                    );

                    ArrowExpressionClauseSyntax CreateBody(bool register) => ArrowExpressionClause
                    (
                        expression: RegisterEvent
                        (
                            targetEvt,
                            MemberAccess(null, TARGET),
                            register,
                            IdentifierName(Value),
                            castTargetTo
                        )
                    );
                }
            }

            [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "There is not dead code.")]
            protected override bool SignatureEquals(IMemberInfo targetMember, IMemberInfo ifaceMember)
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
            public EventInterceptorFactory(IDuckContext context) : base(context) { }
        }
    }
}
