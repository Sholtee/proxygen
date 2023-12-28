/********************************************************************************
* DuckSyntaxFactory.EventInterceptorFactory.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

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
                // Check the visibility
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

            static bool SignatureEquals(IEventInfo targetEvent, IEventInfo ifaceEvent)
            {
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
        /// <code>
        /// event TDelegate IFoo&lt;System.Int32&gt;.Event    
        /// {                                           
        ///   [MethodImplAttribute(AggressiveInlining)] 
        ///   add => Target.Event += value;             
        ///   [MethodImplAttribute(AggressiveInlining)] 
        ///   remove => Target.Event -= value;         
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo targetEvt)
        {
            IEventInfo ifaceEVt = (IEventInfo) context;

            IMethodInfo accessor = targetEvt.AddMethod ?? targetEvt.RemoveMethod!;

            ITypeInfo? castTargetTo = accessor.AccessModifiers is AccessModifiers.Explicit
                ? accessor.DeclaringInterfaces.Single() // Explicit event can have exactly one declaring interface
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
