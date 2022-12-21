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
        /// <code>
        /// private static readonly MethodContext FxXx = new MethodContext((ITarget target, object[] args) =>  
        /// {                                                                                                 
        ///     return target.Prop;                                                                           
        /// }, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                                                                          
        /// private static readonly MethodContext FyYy = new MethodContext((ITarget target, object[] args) =>  
        /// {                                                                                                
        ///     TValue _value = (TValue) args[0];                                                             
        ///     target.Prop = _value;                                                                         
        ///     return null;                                                                                   
        /// }, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                                                                            
        /// TResult IInterface.Prop                                                                          
        /// {                                                                                                 
        ///     get                                                                                            
        ///     {                                                                                            
        ///         object[] args = new object[] { };                                                          
        ///         return (TResult) Invoke(new InvocationContext(args, FxXx));                               
        ///     }                                                                                            
        ///     set                                                                                         
        ///     {                                                                                            
        ///         object[] args = new object[] { value };                                                  
        ///         Invoke(new InvocationContext(args, FyYy));                                               
        ///     }                                                                                              
        /// }
        /// </code>
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
                                                static (local, _) => ResolveArgument(local),
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