/********************************************************************************
* ProxySyntaxFactory.EventInterceptorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveEvents(object context)
        {
            foreach (IEventInfo evt in InterfaceType.Events)
            {
                if (AlreadyImplemented(evt))
                    continue;

                foreach (MemberDeclarationSyntax member in ResolveEvent(null!, evt))
                {
                    yield return member;
                }
            }
        }

        /// <summary>
        /// event EventType IInterface.Event                                                  <br/>
        /// {                                                                                 <br/>
        ///     add                                                                           <br/>
        ///     {                                                                             <br/>
        ///         static object InvokeTarget(ITarget target, object[] args)                 <br/>
        ///         {                                                                         <br/>
        ///             EventType _value = (EventType) args[0];                               <br/>
        ///             Target.Event += _value;                                               <br/>
        ///             return null;                                                          <br/>
        ///         }                                                                         <br/>
        ///         object[] args = new object[] { value };                                   <br/>
        ///         Invoke(new InvocationContext(args, invokeTarget));                        <br/>
        ///     }                                                                             <br/>
        ///     remove                                                                        <br/>
        ///     {                                                                             <br/>
        ///         static object InvokeTarget(ITarget target, object[] args)                 <br/>
        ///         {                                                                         <br/>
        ///             EventType _value = (EventType) args[0];                               <br/>
        ///             Target.Event -= _value;                                               <br/>
        ///             return null;                                                          <br/>
        ///         }                                                                         <br/>
        ///         object[] args = new object[] { value };                                   <br/>
        ///         Invoke(new InvocationContext(args, invokeTarget, MemberTypes.Event));     <br/>
        ///     }                                                                             <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override IEnumerable<MemberDeclarationSyntax> ResolveEvent(object context, IEventInfo evt)
        {
            yield return ResolveEvent
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

                LocalFunctionStatementSyntax invokeTarget = ResolveInvokeTarget
                (
                    method,
                    (target, args, locals, body) =>
                    {
                        body.Add
                        (
                            ExpressionStatement
                            (
                                RegisterEvent
                                (
                                    evt,
                                    IdentifierName(target.Identifier),
                                    add,
                                    ToIdentifierName
                                    (
                                        locals.Single()!
                                    ),
                                    castTargetTo: evt.DeclaringType
                                )
                            )
                        );
                        body.Add
                        (
                            ReturnNull()
                        );
                    }
                );
                yield return invokeTarget;

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(method);
                yield return argsArray;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        Invoke,
                        target: null,
                        castTargetTo: null,
                        Argument
                        (
                            ResolveObject<InvocationContext>
                            (
                                ToArgument(argsArray),
                                Argument
                                (
                                    IdentifierName(invokeTarget.Identifier)
                                )
                            )
                        )
                    )
                );
            }
        }
    }
}