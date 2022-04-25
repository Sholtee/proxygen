/********************************************************************************
* ProxySyntaxFactory.PropertyInterceptorFactory.cs                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context)
        {
            foreach (IPropertyInfo prop in InterfaceType.Properties)
            {
                if (AlreadyImplemented(prop))
                    continue;

                cls = ResolveProperty(cls, context, prop);
            }

            return cls;
        }

        /// <summary>
        /// private static readonly MethodContext FxXx = new MethodContext((ITarget target, object[] args) =>   <br/>
        /// {                                                                                                   <br/>
        ///     return target.Prop;                                                                             <br/>
        /// });                                                                                                 <br/>
        /// private static readonly MethodContext FyYy = new MethodContext((ITarget target, object[] args) =>   <br/>
        /// {                                                                                                   <br/>
        ///     TValue _value = (TValue) args[0];                                                               <br/> 
        ///     target.Prop = _value;                                                                           <br/>
        ///     return null;                                                                                    <br/>
        /// });                                                                                                 <br/>
        /// TResult IInterface.Prop                                                                             <br/>
        /// {                                                                                                   <br/>
        ///     get                                                                                             <br/>
        ///     {                                                                                               <br/>
        ///         object[] args = new object[] { };                                                           <br/>
        ///         return (TResult) Invoke(new InvocationContext(args, FxXx));                                 <br/>
        ///     }                                                                                               <br/>
        ///     set                                                                                             <br/>
        ///     {                                                                                               <br/>
        ///         object[] args = new object[] { value };                                                     <br/>
        ///         Invoke(new InvocationContext(args, FyYy));                                                  <br/>
        ///     }                                                                                               <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperty(ClassDeclarationSyntax cls, object context, IPropertyInfo property)
        {
            List<MemberDeclarationSyntax> members = new();

            BlockSyntax?
                get = null,
                set = null;

            if (property.GetMethod is not null)
            {
                FieldDeclarationSyntax getCtx = ResolveMethodContext
                (
                    ResolveInvokeTarget
                    (
                        property.GetMethod,
                        (target, args, locals, body) => body.Add
                        (
                            ReturnResult
                            (
                                null,
                                CastExpression
                                (
                                    ResolveType<object>(),
                                    PropertyAccess
                                    (
                                        property,
                                        IdentifierName(target.Identifier),
                                        castTargetTo: property.DeclaringType,
                                        indices: locals.Convert(ResolveArgument)
                                    )
                                )
                            )

                        )
                    )
                );
                members.Add(getCtx);

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(property.GetMethod);

                get = Block
                (
                    argsArray,
                    ReturnResult
                    (
                        property.Type,
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
                                        StaticMemberAccess(cls, getCtx)
                                    )
                                )
                            )
                        )
                    )
                );
            }

            if (property.SetMethod is not null)
            {
                FieldDeclarationSyntax setCtx = ResolveMethodContext
                (
                    ResolveInvokeTarget
                    (
                        property.SetMethod,
                        (target, args, locals, body) =>
                        {
                            body.Add
                            (
                                ExpressionStatement
                                (
                                    expression: AssignmentExpression
                                    (
                                        kind: SyntaxKind.SimpleAssignmentExpression,
                                        left: PropertyAccess
                                        (
                                            property,
                                            IdentifierName(target.Identifier),
                                            castTargetTo: property.DeclaringType,
                                            indices: locals.Convert
                                            (
                                                (local, _) => ResolveArgument(local),
                                                (_, i) => i == locals.Count - 1
                                            )
                                        ),
                                        right: ResolveIdentifierName(locals[locals.Count - 1])
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
                members.Add(setCtx);

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(property.SetMethod);

                set = Block
                (
                    argsArray,
                    ExpressionStatement
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
                                        StaticMemberAccess(cls, setCtx)
                                    )
                                )
                            )
                        )
                    )
                );
            }

            members.Add
            (
                property.Indices.Some() 
                    ? ResolveIndexer(property, get, set, false)
                    : ResolveProperty(property, get, set, false)
            );

            return cls.WithMembers
            (
                cls.Members.AddRange(members)
            );
        }
    }
}