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
        /// TResult IInterface.Prop                                                                           <br/>
        /// {                                                                                                 <br/>
        ///     get                                                                                           <br/>
        ///     {                                                                                             <br/>
        ///         object[] args = new object[] { };                                                         <br/>
        ///         Func[object] invokeTarget = () =>                                                         <br/>
        ///         {                                                                                         <br/>
        ///             return Target.Prop;                                                                   <br/>
        ///         };                                                                                        <br/>
        ///         return (TResult) Invoke(new InvocationContext(args, invokeTarget, MemberTypes.Property)); <br/>
        ///     }                                                                                             <br/>
        ///     set                                                                                           <br/>
        ///     {                                                                                             <br/>
        ///         object[] args = new object[] {value};                                                     <br/>
        ///         Func[object] invokeTarget = () =>                                                         <br/>
        ///         {                                                                                         <br/>
        ///           TResult cb_value = (TResult) args[0];                                                   <br/>
        ///           Target.Prop = cb_value;                                                                 <br/>
        ///           return null;                                                                            <br/>
        ///         };                                                                                        <br/>
        ///         Invoke(new InvocationContext(args, invokeTarget, MemberTypes.Property));                  <br/>
        ///     }                                                                                             <br/>
        /// }
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override BasePropertyDeclarationSyntax ResolveProperty(object context, IPropertyInfo property)
        {
            return BuildProperty
            (
                property.Indices.Some() ? DeclareIndexer : DeclareProperty
            );

            IEnumerable<StatementSyntax> BuildGet()
            {
                if (property.GetMethod is null)
                    yield break;

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(property.GetMethod);
                yield return argsArray;

                LocalDeclarationStatementSyntax invokeTarget = DeclareLocal<Func<object>>
                (
                    nameof(invokeTarget),
                    DeclareCallback
                    (
                        argsArray,
                        property.GetMethod,
                        (locals, body) => body.Add
                        (
                            ReturnResult
                            (
                                null,
                                CastExpression
                                (
                                    CreateType<object>(),
                                    PropertyAccess(property, MemberAccess(null, Target), null, locals.Convert(ToArgument))
                                )
                            )

                        )
                    )
                );
                yield return invokeTarget;

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
                            CreateObject<InvocationContext>
                            (
                                ToArgument(argsArray),
                                ToArgument(invokeTarget),
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

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(property.SetMethod);
                yield return argsArray;

                LocalDeclarationStatementSyntax invokeTarget = DeclareLocal<Func<object>>
                (
                    nameof(invokeTarget),
                    DeclareCallback
                    (
                        argsArray,
                        property.SetMethod,
                        (locals, body) =>
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
                                            MemberAccess(null, Target),
                                            null,
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
                    )
                );
                yield return invokeTarget;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        Invoke,
                        target: null,
                        castTargetTo: null,
                        Argument
                        (
                            CreateObject<InvocationContext>
                            (
                                ToArgument(argsArray),
                                ToArgument(invokeTarget),
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