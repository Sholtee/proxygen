/********************************************************************************
* ProxySyntaxFactory.EventInterceptorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
        /// <summary>
        /// event EventType IInterface.Event                                            <br/>
        /// {                                                                           <br/>
        ///     add                                                                     <br/>
        ///     {                                                                       <br/>
        ///         object[] args = new object[] {value};                               <br/>
        ///         InvokeTarget = () =>                                                <br/>
        ///         {                                                                   <br/>
        ///             EventType cb_value = (EventType) args[a];                       <br/>
        ///             Target.Event += cb_value;                                       <br/>
        ///         };                                                                  <br/>
        ///         EventInfo evt = ResolveEvent(InvokeTarget);                         <br/>
        ///         Invoke(evt.AddMethod, args, evt);                                   <br/>
        ///     }                                                                       <br/>
        ///     remove                                                                  <br/>
        ///     {                                                                       <br/>
        ///         object[] args = new object[] {value};                               <br/>
        ///         InvokeTarget = () =>                                                <br/>
        ///         {                                                                   <br/>
        ///             EventType cb_value = (EventType) args[a];                       <br/>
        ///             Target.Event -= cb_value;                                       <br/>
        ///         };                                                                  <br/>
        ///         EventInfo evt = ResolveEvent(InvokeTarget);                         <br/>
        ///         Invoke(evt.RemoveMethod, args, evt);                                <br/>
        ///     }                                                                       <br/>
        /// }
        /// </summary>
        internal sealed class EventInterceptorFactory : ProxyMemberSyntaxFactory
        {
            private readonly IMethodInfo
                RESOLVE_EVENT;

            private IEnumerable<StatementSyntax> Build(IEventInfo member, bool add) 
            {
                IMethodInfo targetMethod = add ? member.AddMethod : member.RemoveMethod;

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(targetMethod);
                yield return argsArray;

                yield return AssignCallback
                (
                    DeclareCallback
                    (
                        argsArray,
                        targetMethod,
                        (locals, result) => new StatementSyntax[]
                        {
                            ExpressionStatement
                            (
                                RegisterEvent(member, MemberAccess(null, TARGET), add, ToIdentifierName(locals.Single()))
                            )
                        }
                    )
                );

                LocalDeclarationStatementSyntax evt = DeclareLocal<EventInfo>(nameof(evt), InvokeMethod
                (
                    RESOLVE_EVENT,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        expression: PropertyAccess(INVOKE_TARGET, null, null)
                    )
                ));

                yield return evt;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        INVOKE,
                        target: null,
                        castTargetTo: null,
                        arguments: new ExpressionSyntax[]
                        {
                            SimpleMemberAccess // evt.[Add|Remove]Method
                            (
                                ToIdentifierName(evt),
                                add ? nameof(EventInfo.AddMethod) : nameof(EventInfo.RemoveMethod)
                            ),
                            ToIdentifierName(argsArray), // args
                            ToIdentifierName(evt) // evt
                        }.Select(Argument).ToArray()
                    )
                );
            }

            public EventInterceptorFactory(ProxySyntaxFactory owner) : base(owner)
            {
                RESOLVE_EVENT = Owner.BaseInterceptorType
                    .Methods
                    .Single(met => met.Name == nameof(InterfaceInterceptor<object>.ResolveEvent));
            }

            protected override IEnumerable<MemberDeclarationSyntax> Build() => Owner.InterfaceType
                .Events
                .Where(evt => !AlreadyImplemented(evt))
                .Select(evt => DeclareEvent
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
                ));
        }
    }
}