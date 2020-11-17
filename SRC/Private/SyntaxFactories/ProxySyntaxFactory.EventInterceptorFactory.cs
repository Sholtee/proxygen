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
    internal partial class ProxySyntaxFactory<TInterface, TInterceptor> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
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
        internal sealed class EventInterceptorFactory : InterceptorFactoryBase
        {
            private static readonly MethodInfo
                RESOLVE_EVENT = (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.ResolveEvent(default!));

            private IEnumerable<StatementSyntax> Build(EventInfo member, bool add) 
            {
                MethodInfo targetMethod = add ? member.AddMethod : member.RemoveMethod;

                LocalDeclarationStatementSyntax argsArray = Owner.CreateArgumentsArray(targetMethod);
                yield return argsArray;

                yield return Owner.AssignCallback
                (
                    Owner.DeclareCallback
                    (
                        argsArray,
                        targetMethod,
                        (locals, result) => new StatementSyntax[]
                        {
                            ExpressionStatement
                            (
                                Owner.RegisterEvent(member, Owner.TARGET, add, ToIdentifierName(locals.Single()))
                            )
                        }
                    )
                );

                LocalDeclarationStatementSyntax evt = Owner.DeclareLocal(typeof(EventInfo), nameof(evt), Owner.InvokeMethod
                (
                    RESOLVE_EVENT,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        expression: Owner.PropertyAccess(INVOKE_TARGET, null, null)
                    )
                ));

                yield return evt;

                yield return ExpressionStatement
                (
                    Owner.InvokeMethod
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

            public EventInterceptorFactory(ProxySyntaxFactory<TInterface, TInterceptor> owner) : base(owner) { }

            public override bool IsCompatible(MemberInfo member) => member is EventInfo evt && evt.DeclaringType.IsInterface && !AlreadyImplemented(evt);

            public override MemberDeclarationSyntax Build(MemberInfo member)
            {
                EventInfo evt = (EventInfo) member;

                return Owner.DeclareEvent
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
            }
        }
    }
}