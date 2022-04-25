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
        protected override ClassDeclarationSyntax ResolveEvents(ClassDeclarationSyntax cls, object context)
        {
            foreach (IEventInfo evt in InterfaceType.Events)
            {
                if (AlreadyImplemented(evt))
                    continue;

                cls = ResolveEvent(cls, context, evt);
            }

            return cls;
        }

        /// <summary>
        /// private static readonly MethodContext FXxX = new MethodContext((ITarget target, object[] args) => <br/>
        /// {                                                                                                 <br/>
        ///     EventType _value = (EventType) args[0];                                                       <br/>
        ///     Target.Event += _value;                                                                       <br/>
        ///     return null;                                                                                  <br/>
        /// });                                                                                               <br/>
        /// private static readonly MethodContext FYyY = new MethodContext((ITarget target, object[] args) => <br/>
        /// {                                                                                                 <br/>
        ///     EventType _value = (EventType) args[0];                                                       <br/>
        ///     Target.Event -= _value;                                                                       <br/>
        ///     return null;                                                                                  <br/>
        /// });                                                                                               <br/>
        /// event EventType IInterface.Event                                                                  <br/>
        /// {                                                                                                 <br/>
        ///     add                                                                                           <br/>
        ///     {                                                                                             <br/>
        ///         object[] args = new object[] { value };                                                   <br/>
        ///         Invoke(new InvocationContext(args, FXxX));                                                <br/>
        ///     }                                                                                             <br/>
        ///     remove                                                                                        <br/>
        ///     {                                                                                             <br/>
        ///         object[] args = new object[] { value };                                                   <br/>
        ///         Invoke(new InvocationContext(args, FYyY));                                                <br/>
        ///     }                                                                                             <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo evt)
        {
            List<MemberDeclarationSyntax> members = new();

            BlockSyntax?
                add = null,
                remove = null;

            if (evt.AddMethod is not null)
            {
                FieldDeclarationSyntax addCtx = BuildField(true, evt.AddMethod);
                members.Add(addCtx);

                add = Block
                (
                    BuildBody(true, evt.AddMethod, addCtx)
                );
            }

            if (evt.RemoveMethod is not null)
            {
                FieldDeclarationSyntax removeCtx = BuildField(false, evt.RemoveMethod);
                members.Add(removeCtx);

                remove = Block
                (
                    BuildBody(false, evt.RemoveMethod, removeCtx)
                );
            }

            members.Add
            (
                ResolveEvent(evt, add, remove)
            );

            return cls.WithMembers
            (
                cls.Members.AddRange(members)
            );

            FieldDeclarationSyntax BuildField(bool add, IMethodInfo method) => ResolveMethodContext
            (
                ResolveInvokeTarget
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
                                    ResolveIdentifierName
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
                )
            );

            IEnumerable<StatementSyntax> BuildBody(bool add, IMethodInfo method, FieldDeclarationSyntax fld)
            {
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
                                ResolveArgument(argsArray),
                                Argument
                                (
                                    StaticMemberAccess(cls, fld)
                                )
                            )
                        )
                    )
                );
            }
        }
    }
}