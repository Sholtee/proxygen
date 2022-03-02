/********************************************************************************
* ProxySyntaxFactory.EventInterceptorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override IEnumerable<EventDeclarationSyntax> ResolveEvents(object context)
        {
            foreach (IEventInfo evt in InterfaceType.Events)
            {
                if (AlreadyImplemented(evt))
                    continue;

                yield return ResolveEvent(null!, evt);
            }
        }

        /// <summary>
        /// event EventType IInterface.Event                                                  <br/>
        /// {                                                                                 <br/>
        ///     add                                                                           <br/>
        ///     {                                                                             <br/>
        ///         object[] args = new object[] {value};                                     <br/>
        ///         Func[object] invokeTarget = () =>                                         <br/>
        ///         {                                                                         <br/>
        ///             EventType cb_value = (EventType) args[a];                             <br/>
        ///             Target.Event += cb_value;                                             <br/>
        ///             return null;                                                          <br/>
        ///         };                                                                        <br/>
        ///         Invoke(new InvocationContext(args, invokeTarget, MemberTypes.Event));     <br/>
        ///     }                                                                             <br/>
        ///     remove                                                                        <br/>
        ///     {                                                                             <br/>
        ///         object[] args = new object[] {value};                                     <br/>
        ///         Func[object] invokeTarget = () =>                                         <br/>
        ///         {                                                                         <br/>
        ///             EventType cb_value = (EventType) args[a];                             <br/>
        ///             Target.Event -= cb_value;                                             <br/>
        ///             return null;                                                          <br/>
        ///         };                                                                        <br/>
        ///         Invoke(new InvocationContext(args, invokeTarget, MemberTypes.Event));     <br/>
        ///     }                                                                             <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override EventDeclarationSyntax ResolveEvent(object context, IEventInfo evt)
        {
            return DeclareEvent
            (
                @event: evt,
                addBody: Block
                (
                    BuildBody(add: true)
                ),
                removeBody: Block
                (
                    BuildBody(add: false)
                )
            );

            IEnumerable<StatementSyntax> BuildBody(bool add)
            {
                IMethodInfo? method = add ? evt.AddMethod : evt.RemoveMethod;
                if (method is null)
                    yield break;

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(method);
                yield return argsArray;

                LocalDeclarationStatementSyntax invokeTarget = DeclareLocal<Func<object>>
                (
                    nameof(invokeTarget),
                    DeclareCallback
                    (
                        argsArray,
                        method,
                        (locals, body) =>
                        {
                            body.Add
                            (
                                ExpressionStatement
                                (
                                    RegisterEvent
                                    (
                                        evt,
                                        MemberAccess(null, Target),
                                        add,
                                        ToIdentifierName
                                        (
                                            locals.Single()!
                                        )
                                    )
                                )
                            );
                            body.Add
                            (
                                ReturnNull()
                            );
                        }
                    )
                );
                yield return invokeTarget;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        Invoke,
                        target: null,
                        castTargetTo: null,
                        Argument
                        (
                            CreateObject<InvocationContext>
                            (
                                ToArgument(argsArray),
                                ToArgument(invokeTarget),
                                Argument
                                (
                                    EnumAccess(MemberTypes.Event)
                                )
                            )
                        )
                    )
                );
            }
        }
    }
}