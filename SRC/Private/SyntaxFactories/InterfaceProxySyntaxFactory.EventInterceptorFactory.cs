/********************************************************************************
* InterfaceProxySyntaxFactory.EventInterceptorFactory.cs                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class InterfaceProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvents(ClassDeclarationSyntax cls, object context)
        {
            foreach (IEventInfo evt in InterfaceType.Events)
            {
                if (AlreadyImplemented(evt, InterceptorType.Events, SignatureEquals))
                    continue;

                cls = ResolveEvent(cls, context, evt);
            }

            return cls;

            static bool SignatureEquals(IEventInfo targetEvent, IEventInfo ifaceEvent)
            {
                if (ifaceEvent.AddMethod is not null)
                {
                    if (targetEvent.AddMethod?.SignatureEquals(ifaceEvent.AddMethod) is not true)
                        return false;
                }

                if (ifaceEvent.RemoveMethod is not null)
                {
                    if (targetEvent.RemoveMethod?.SignatureEquals(ifaceEvent.RemoveMethod) is not true)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// <code>
        /// private static readonly InterfaceInterceptionContext FXxX = new InterfaceInterceptionContext((object target, object[] args) =>
        /// {                                                                                                
        ///     EventType _value = (EventType) args[0];                                                      
        ///     ((ITarget)target).Event += _value;                                                                      
        ///     return null;                                                                                  
        /// }, CALL_INDEX, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                                                                              
        /// private static readonly InterfaceInterceptionContext FYyY = new InterfaceInterceptionContext((object target, object[] args) => 
        /// {                                                                                                 
        ///     EventType _value = (EventType) args[0];                                                      
        ///     ((ITarget)target).Event -= _value;                                                                       
        ///     return null;                                                                                 
        /// }, CALL_INDEX, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                                                                             
        /// event EventType IInterface.Event                                                                  
        /// {                                                                                                 
        ///     add                                                                                          
        ///     {                                                                                           
        ///         object[] args = new object[] { value };                                                
        ///         Invoke(new InterfaceInvocationContext(args, FXxX));                                               
        ///     }                                                                                            
        ///     remove                                                                                       
        ///     {                                                                                          
        ///         object[] args = new object[] { value };                                             
        ///         Invoke(new InterfaceInvocationContext(args, FYyY));                                              
        ///     }                                                                                           
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo evt)
        {
            //
            // For now, we only have call-index of 0
            //

            const int CALL_INDEX = 0;

            FieldDeclarationSyntax
                addContext = BuildField(true),
                removeContext = BuildField(false);

            List<MemberDeclarationSyntax> members = 
            [
                addContext,
                removeContext,
                ResolveEvent
                (
                    evt,
                    BuildBody(evt.AddMethod, addContext),
                    BuildBody(evt.RemoveMethod, removeContext)
                )
            ];

            return cls.WithMembers
            (
                cls.Members.AddRange(members)
            );

            FieldDeclarationSyntax BuildField(bool add)
            {
                IMethodInfo backingMethod = add ? evt.AddMethod : evt.RemoveMethod;
                return ResolveMethodContext
                (
                    backingMethod.GetMD5HashCode(),
                    ResolveInvokeTarget
                    (
                        backingMethod,
                        hasTarget: true,
                        (paramz, locals) => RegisterEvent
                        (
                            evt,
                            IdentifierName(paramz[0].Identifier),
                            add,
                            ResolveIdentifierName
                            (
                                locals.Single()
                            ),
                            castTargetTo: evt.DeclaringType
                        )
                    ),
                    CALL_INDEX
                );
            }

            BlockSyntax BuildBody(IMethodInfo method, FieldDeclarationSyntax field)
            {
                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(method);

                StatementSyntax[] statements =
                [
                    argsArray,
                    ExpressionStatement
                    (
                        InvokeMethod
                        (
                            Invoke,
                            arguments: Argument
                            (
                                ResolveObject<InterfaceInvocationContext>
                                (
                                    ResolveArgument(argsArray),
                                    Argument
                                    (
                                        StaticMemberAccess(cls, field)
                                    )
                                )
                            )
                        )
                    )
                ];

                return Block(statements);
            }
        }
    }
}