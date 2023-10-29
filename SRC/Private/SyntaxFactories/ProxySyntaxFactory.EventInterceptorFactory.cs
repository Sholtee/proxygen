/********************************************************************************
* ProxySyntaxFactory.EventInterceptorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

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

                //
                // Starting from .NET7.0 interfaces may have abstract static members
                //

                if (evt.IsAbstract && evt.IsStatic)
                    throw new NotSupportedException(Resources.ABSTRACT_STATIC_NOT_SUPPORTED);

                cls = ResolveEvent(cls, context, evt);
            }

            return cls;
        }

        /// <summary>
        /// <code>
        /// private static readonly MethodContext FXxX = new MethodContext((ITarget target, object[] args) =>
        /// {                                                                                                
        ///     EventType _value = (EventType) args[0];                                                      
        ///     Target.Event += _value;                                                                      
        ///     return null;                                                                                  
        /// }, CALL_INDEX, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                                                                              
        /// private static readonly MethodContext FYyY = new MethodContext((ITarget target, object[] args) => 
        /// {                                                                                                 
        ///     EventType _value = (EventType) args[0];                                                      
        ///     Target.Event -= _value;                                                                       
        ///     return null;                                                                                 
        /// }, CALL_INDEX, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                                                                             
        /// event EventType IInterface.Event                                                                  
        /// {                                                                                                 
        ///     add                                                                                          
        ///     {                                                                                           
        ///         object[] args = new object[] { value };                                                
        ///         Invoke(new InvocationContext(args, FXxX));                                               
        ///     }                                                                                            
        ///     remove                                                                                       
        ///     {                                                                                          
        ///         object[] args = new object[] { value };                                             
        ///         Invoke(new InvocationContext(args, FYyY));                                              
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
                                        locals.Single()
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
                ),
                CALL_INDEX
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