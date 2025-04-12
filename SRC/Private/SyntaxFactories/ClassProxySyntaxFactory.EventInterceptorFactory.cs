/********************************************************************************
* ClassProxySyntaxFactory.EventInterceptorFactory.cs                            *
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
    internal partial class ClassProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvents(ClassDeclarationSyntax cls, object context)
        {
            foreach (IEventInfo evt in BaseType.Events)
            {
                IMethodInfo targetMethod = evt.AddMethod ?? evt.RemoveMethod;  
                if (targetMethod.IsAbstract || targetMethod.IsVirtual)
                    cls = ResolveEvent(cls, context, evt);
            }

            return cls;
        }

        /// <summary>
        /// <code>
        /// private static ExtendedMemberInfo FXxX;
        /// public override TCallback Event
        /// {
        ///     add
        ///     {
        ///         CurrentMethod.GetBase(ref FXxX);
        ///     
        ///         object[] args = new object[] {value};
        ///         
        ///         FInterceptor.Invoke
        ///         (
        ///             new ClassInvocationContext
        ///             (
        ///                 FXxX,
        ///                 args =>
        ///                 {
        ///                     TCallback cb_value = (TCallback) args[0];
        ///                     base.Event += cb_value;         
        ///                     return null;    
        ///                 },
        ///                 args,
        ///                 new Type[] {}
        ///             )
        ///         ); 
        ///     }
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveEvent(ClassDeclarationSyntax cls, object context, IEventInfo evt)
        {
            if (!IsVisible(evt.AddMethod) || !IsVisible(evt.RemoveMethod))
                return cls;

            FieldDeclarationSyntax
                addField = ResolveField<ExtendedMemberInfo>
                (
                    $"F{evt.AddMethod.GetMD5HashCode()}",
                    @readonly: false
                ),
                removeField = ResolveField<ExtendedMemberInfo>
                (
                    $"F{evt.RemoveMethod.GetMD5HashCode()}",
                    @readonly: false
                );

            return cls.AddMembers
            (
                addField,
                removeField,
                ResolveEvent
                (
                    evt,
                    Block
                    (
                        BuildBody(true)
                    ),
                    Block
                    (
                        BuildBody(false)
                    )
                )
            );

            IEnumerable<StatementSyntax> BuildBody(bool add)
            {
                FieldDeclarationSyntax field = add ? addField : removeField;
                IMethodInfo backingMethod = add ? evt.AddMethod : evt.RemoveMethod;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        FGetBase,
                        arguments: Argument
                        (
                            StaticMemberAccess(cls, field)
                        )
                    )
                );

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(backingMethod);
                yield return argsArray;

                yield return ExpressionStatement
                (
                    InvokeInterceptor<ClassInvocationContext>
                    (

                        Argument
                        (
                            StaticMemberAccess(cls, field)
                        ),
                        Argument
                        (
                            backingMethod.IsAbstract ? ResolveNotImplemented() : ResolveInvokeTarget
                            (
                                backingMethod,
                                hasTarget: false,
                                (_, locals) => RegisterEvent
                                (
                                    evt,
                                    target: BaseExpression(),
                                    add,
                                    ResolveIdentifierName
                                    (
                                        locals.Single()
                                    )
                                )
                            )
                        ),
                        Argument
                        (
                            ResolveIdentifierName(argsArray)
                        ),
                        Argument
                        (
                            ResolveArray<Type>([])
                        )
                    )
                );
            }
        }
    }
}