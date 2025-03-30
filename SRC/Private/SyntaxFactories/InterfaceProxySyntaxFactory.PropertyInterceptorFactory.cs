/********************************************************************************
* InterfaceProxySyntaxFactory.PropertyInterceptorFactory.cs                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class InterfaceProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context)
        {
            foreach (IPropertyInfo prop in InterfaceType.Properties)
            {
                if (AlreadyImplemented(prop, InterceptorType.Properties, SignatureEquals))
                    continue;

                cls = ResolveProperty(cls, context, prop);
            }

            return cls;

            static bool SignatureEquals(IPropertyInfo targetProp, IPropertyInfo ifaceProp)
            {
                //
                // We allow the implementation to declare a getter or setter that is not required by the interface.
                //

                if (ifaceProp.GetMethod is not null)
                {
                    if (targetProp.GetMethod?.SignatureEquals(ifaceProp.GetMethod) is not true)
                        return false;
                }

                if (ifaceProp.SetMethod is not null)
                {
                    if (targetProp.SetMethod?.SignatureEquals(ifaceProp.SetMethod) is not true)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// <code>
        /// private static readonly InterfaceInterceptionContext FxXx = new InterfaceInterceptionContext(static (ITarget target, object[] args) =>  
        /// {                                                                                                 
        ///     return target.Prop;                                                                           
        /// }, CALL_INDEX, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                                                                          
        /// private static readonly InterfaceInterceptionContext FyYy = new InterfaceInterceptionContext(static (ITarget target, object[] args) =>  
        /// {                                                                                                
        ///     TValue _value = (TValue) args[0];                                                             
        ///     target.Prop = _value;                                                                         
        ///     return null;                                                                                   
        /// }, CALL_INDEX, InterfaceMap&lt;TInterface, TTarget&gt;.Value | null);                                                                                            
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
            //
            // For now, we only have call-index of 0
            //

            const int CALL_INDEX = 0;

            List<MemberDeclarationSyntax> members = new();

            BlockSyntax?
                get = null,
                set = null;

            if (property.GetMethod is not null)
            {
                FieldDeclarationSyntax getCtx = ResolveMethodContext
                (
                    property.GetMethod.GetMD5HashCode(),
                    ResolveInvokeTarget
                    (
                        property.GetMethod,
                        hasTarget: true,
                        (paramz, locals) => PropertyAccess
                        (
                            property,
                            IdentifierName(paramz[0].Identifier),
                            castTargetTo: property.DeclaringType,
                            indices: locals.Select(ResolveArgument)
                        )
                    ),
                    CALL_INDEX
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
                            arguments: Argument
                            (
                                ResolveObject<InterfaceInvocationContext>
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
                    property.SetMethod.GetMD5HashCode(),
                    ResolveInvokeTarget
                    (
                        property.SetMethod,
                        hasTarget: true,
                        (paramz, locals) => AssignmentExpression // target.Prop = _value
                        (
                            kind: SyntaxKind.SimpleAssignmentExpression,
                            left: PropertyAccess
                            (
                                property,
                                IdentifierName(paramz[0].Identifier),
                                castTargetTo: property.DeclaringType,
                                indices: locals
                                    .Take(locals.Count - 1)
                                    .Select(ResolveArgument)
                            ),
                            right: ResolveIdentifierName(locals[locals.Count - 1])
                        )
                    ),
                    CALL_INDEX
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
                            arguments: Argument
                            (
                                ResolveObject<InterfaceInvocationContext>
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
                property.Indices.Any() 
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