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
        internal sealed class EventInterceptorFactory : ProxyMemberSyntaxFactory
        {
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
                                ToArgument(argsArray),
                                ToArgument(invokeTarget),
                                Argument(EnumAccess(MemberTypes.Event))
                            )
                        )
                    )
                );
            }

            public EventInterceptorFactory(IProxyContext context) : base(context)
            {
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