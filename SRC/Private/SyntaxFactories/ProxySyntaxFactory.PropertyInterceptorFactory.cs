/********************************************************************************
* ProxySyntaxFactory.PropertyInterceptorFactory.cs                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

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
        protected override IEnumerable<BasePropertyDeclarationSyntax> ResolveProperties(object context)
        {
            foreach (IPropertyInfo prop in InterfaceType.Properties)
            {
                if (AlreadyImplemented(prop))
                    continue;

                yield return ResolveProperty(null!, prop);
            }
        }

        /// <summary>
        /// TResult IInterface.Prop                                                                                   <br/>
        /// {                                                                                                         <br/>
        ///     get                                                                                                   <br/>
        ///     {                                                                                                     <br/>
        ///         static object InvokeTarget(ITarget target, object[] args)                                         <br/>
        ///         {                                                                                                 <br/>
        ///             return target.Prop;                                                                           <br/>
        ///         }                                                                                                 <br/>
        ///         object[] args = new object[] { };                                                                 <br/>
        ///         return (TResult) Invoke(new InvocationContext(args, InvokeTarget, MemberTypes.Property));         <br/>
        ///     }                                                                                                     <br/>
        ///     set                                                                                                   <br/>
        ///     {                                                                                                     <br/>
        ///         static object InvokeTarget(ITarget target, object[] args)                                         <br/>
        ///         {                                                                                                 <br/>
        ///           TValue _value = (TValue) args[0];                                                               <br/> 
        ///           target.Prop = _value;                                                                           <br/>
        ///           return null;                                                                                    <br/>
        ///         }                                                                                                 <br/>
        ///         object[] args = new object[] { value };                                                           <br/>
        ///         Invoke(new InvocationContext(args, InvokeTarget, MemberTypes.Property));                          <br/>
        ///     }                                                                                                     <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override BasePropertyDeclarationSyntax ResolveProperty(object context, IPropertyInfo property)
        {
            return BuildProperty
            (
                property.Indices.Some() ? ResolveIndexer : ResolveProperty
            );

            IEnumerable<StatementSyntax> BuildGet()
            {
                if (property.GetMethod is null)
                    yield break;

                LocalFunctionStatementSyntax invokeTarget = ResolveInvokeTarget
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
                                    locals.Convert(ToArgument)
                                )
                            )
                        )

                    )
                );
                yield return invokeTarget;

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(property.GetMethod);
                yield return argsArray;

                yield return ReturnResult
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
                                ToArgument(argsArray),
                                Argument
                                (
                                    IdentifierName
                                    (
                                        invokeTarget.Identifier
                                    )
                                ),
                                Argument
                                (
                                    EnumAccess(MemberTypes.Property)
                                )
                            )
                        )
                    )
                );
            }

            IEnumerable<StatementSyntax> BuildSet()
            {
                if (property.SetMethod is null)
                    yield break;

                LocalFunctionStatementSyntax invokeTarget = ResolveInvokeTarget
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
                                        locals.Convert
                                        (
                                            (local, _) => ToArgument(local),
                                            (_, i) => i == locals.Count - 1
                                        )
                                    ),
                                    right: ToIdentifierName(locals[locals.Count - 1])
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

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(property.SetMethod);
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
                                    IdentifierName
                                    (
                                        invokeTarget.Identifier
                                    )
                                ),
                                Argument
                                (
                                    EnumAccess(MemberTypes.Property)
                                )
                            )
                        )
                    )
                );
            }

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            BasePropertyDeclarationSyntax BuildProperty(Func<IPropertyInfo, CSharpSyntaxNode?, CSharpSyntaxNode?, bool, BasePropertyDeclarationSyntax> fact) => fact
            (
                property,
                Block
                (
                    BuildGet()
                ),
                Block
                (
                    BuildSet()
                ),
                false
            );
        }
    }
}