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
            foreach (IEventInfo ifaceEvt in FInterfaceType.Events)
            {
                IEventInfo targetEvt = GetTargetMember
                (
                    ifaceEvt,
                    TargetType!.Events,
                    static (targetEvent, ifaceEvent) =>
                        targetEvent.AddMethod.SignatureEquals(ifaceEvent.AddMethod, ignoreVisibility: true) &&
                        targetEvent.RemoveMethod.SignatureEquals(ifaceEvent.RemoveMethod, ignoreVisibility: true)
                );

                cls = ResolveEvent(cls, ifaceEvt, targetEvt);
            }

            return cls;
        }

        /// <summary>
        /// <code>
        /// event TDelegate IFoo&lt;System.Int32&gt;.Event    
        /// {                                            
        ///   add => Target.Event += value;             
        ///   remove => Target.Event -= value;         
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo targetEvt)
        {
            IEventInfo ifaceEvt = (IEventInfo) context;

            Visibility.Check(targetEvt, ContainingAssembly);

            //
            // Starting from .NET 5.0 interface members may have visibility.
            //

            Visibility.Check(ifaceEvt, ContainingAssembly);

            IMethodInfo accessor = targetEvt.AddMethod ?? targetEvt.RemoveMethod!;

            //
            // Explicit members cannot be accessed directly
            //

            ITypeInfo? castTargetTo = accessor.AccessModifiers is AccessModifiers.Explicit
                ? accessor.DeclaringInterfaces.Single() // Explicit event can have exactly one declaring interface
                : null;

            return cls.AddMembers
            (
                ResolveEvent
                (
                    ifaceEvt,
                    addBody: CreateBody(register: true),
                    removeBody: CreateBody(register: false)
                )
            );

            ArrowExpressionClauseSyntax CreateBody(bool register) => ArrowExpressionClause
            (
                expression: RegisterEvent
                (
                    targetEvt,
                    GetTarget(),
                    register,
                    FValue,
                    castTargetTo
                )
            );
        }
    }
}
