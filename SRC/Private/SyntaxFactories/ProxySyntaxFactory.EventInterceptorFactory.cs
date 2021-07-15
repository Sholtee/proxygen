/********************************************************************************
* ProxySyntaxFactory.EventInterceptorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
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
        ///         EventInfo evt = ResolveEvent(invokeTarget);                               <br/>
        ///         Invoke(new InvocationContext(evt.AddMethod, args, evt, invokeTarget));    <br/>
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
        ///         EventInfo evt = ResolveEvent(invokeTarget);                               <br/>
        ///         Invoke(new InvocationContext(evt.RemoveMethod, args, evt, invokeTarget)); <br/>
        ///     }                                                                             <br/>
        /// }
        /// </summary>
        internal sealed class EventInterceptorFactory : ProxyMemberSyntaxFactory
        {
            private readonly IMethodInfo
                RESOLVE_EVENT;

            private IEnumerable<StatementSyntax> Build(IEventInfo member, bool add) 
            {
                IMethodInfo? targetMethod = add ? member.AddMethod : member.RemoveMethod;
                if (targetMethod is null)
                    yield break;

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(targetMethod);
                yield return argsArray;

                LocalDeclarationStatementSyntax invokeTarget = DeclareLocal<Func<object>>
                (
                    nameof(invokeTarget),
                    DeclareCallback
                    (
                        argsArray,
                        targetMethod,
                        (locals, body) =>
                        {
                            body.Add
                            (
                                ExpressionStatement
                                (
                                    RegisterEvent(member, MemberAccess(null, TARGET), add, ToIdentifierName(locals.Single()))
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

                LocalDeclarationStatementSyntax evt = DeclareLocal<EventInfo>(nameof(evt), InvokeMethod
                (
                    RESOLVE_EVENT,
                    target: null,
                    castTargetTo: null,
                    ToArgument(invokeTarget)
                ));

                yield return evt;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        INVOKE,
                        target: null,
                        castTargetTo: null,
                        Argument
                        (
                            CreateObject<InvocationContext>
                            (
                                Argument
                                (
                                    SimpleMemberAccess // evt.[Add|Remove]Method
                                    (
                                        ToIdentifierName(evt),
                                        add ? nameof(EventInfo.AddMethod) : nameof(EventInfo.RemoveMethod)
                                    )
                                ),
                                ToArgument(argsArray),
                                ToArgument(evt),
                                ToArgument(invokeTarget)
                            )
                        )
                    )
                );
            }

            public EventInterceptorFactory(IProxyContext context) : base(context)
            {
                RESOLVE_EVENT = Context.InterceptorType.Methods.Single
                (
                    met => met.SignatureEquals
                    (
                        MetadataMethodInfo.CreateFrom
                        (
                            (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<object>.ResolveEvent(default!))
                        )
                    )
                );
            }

            protected override IEnumerable<MemberDeclarationSyntax> BuildMembers(CancellationToken cancellation) => Context
                .InterfaceType
                .Events
                .Where(evt => !AlreadyImplemented(evt))
                .Select(evt =>
                {
                    cancellation.ThrowIfCancellationRequested();

                    return DeclareEvent
                    (
                        @event: evt,
                        addBody: Block
                        (
                            Build(evt, add: true)
                        ),
                        removeBody: Block
                        (
                            Build(evt, add: false)
                        )
                    );
                });
        }
    }
}