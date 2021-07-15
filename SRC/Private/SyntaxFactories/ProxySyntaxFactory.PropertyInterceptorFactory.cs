/********************************************************************************
* ProxySyntaxFactory.PropertyInterceptorFactory.cs                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory
    {
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
        ///         PropertyInfo prop = ResolveProperty(invokeTarget);                                        <br/>
        ///         return (TResult) Invoke(new InvocationContext(prop.GetMethod, args, prop, invokeTarget)); <br/>
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
        ///         PropertyInfo prop = ResolveProperty(invokeTarget);                                        <br/>
        ///         Invoke(new InvocationContext(prop.SetMethod, args, prop, invokeTarget));                  <br/>
        ///     }                                                                                             <br/>
        /// }
        /// </summary>
        internal sealed class PropertyInterceptorFactory : ProxyMemberSyntaxFactory
        {
            private readonly IMethodInfo
                RESOLVE_PROPERTY;

            private IEnumerable<StatementSyntax> BuildGet(IPropertyInfo property) 
            {
                if (property.GetMethod is null) yield break;

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
                                    PropertyAccess(property, MemberAccess(null, TARGET), null, locals.Select(ToArgument))
                                )
                            )

                        )
                    )
                );
                yield return invokeTarget;

                LocalDeclarationStatementSyntax prop = DeclareLocal<PropertyInfo>
                (
                    nameof(prop),
                    InvokeMethod
                    (
                        RESOLVE_PROPERTY,
                        target: null,
                        castTargetTo: null,
                        ToArgument(invokeTarget)
                    )
                );
                yield return prop;

                yield return ReturnResult
                (
                    property.Type,
                    InvokeMethod
                    (
                        INVOKE,
                        target: null,
                        castTargetTo: null,
                        Argument
                        (
                            CreateObject<InvocationContext>
                            (
                                Argument
                                (
                                    SimpleMemberAccess // prop.GetMethod
                                    (
                                        ToIdentifierName(prop),
                                        nameof(PropertyInfo.GetMethod)
                                    )
                                ),
                                ToArgument(argsArray),
                                ToArgument(prop),
                                ToArgument(invokeTarget)
                            )
                        )
                    )
                );
            }

            private IEnumerable<StatementSyntax> BuildSet(IPropertyInfo property)
            {
                if (property.SetMethod is null) yield break;

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
                                        left: PropertyAccess(property, MemberAccess(null, TARGET), null, locals
#if NETSTANDARD2_1_OR_GREATER
                                            .SkipLast(1)                                          
#else
                                            .Take(locals.Count - 1)
#endif
                                            .Select(ToArgument)),
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

                LocalDeclarationStatementSyntax prop = DeclareLocal<PropertyInfo>
                (
                    nameof(prop),
                    InvokeMethod
                    (
                        RESOLVE_PROPERTY,
                        target: null,
                        castTargetTo: null,
                        ToArgument(invokeTarget)
                    )
                );
                yield return prop;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        INVOKE,
                        target: null,
                        castTargetTo: null,
                        Argument
                        (
                            CreateObject<InvocationContext>
                            (
                                Argument
                                (
                                    SimpleMemberAccess // prop.SetMethod
                                    (
                                        ToIdentifierName(prop),
                                        nameof(PropertyInfo.SetMethod)
                                    )
                                ),
                                ToArgument(argsArray),
                                ToArgument(prop),
                                ToArgument(invokeTarget)
                            )
                        )
                    )
                );
            }

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            private MemberDeclarationSyntax BuildProperty(IPropertyInfo property, Func<IPropertyInfo, CSharpSyntaxNode?, CSharpSyntaxNode?, bool, MemberDeclarationSyntax> fact) => fact
            (
                property,
                Block
                (
                    BuildGet(property)
                ),
                Block
                (
                    BuildSet(property)
                ),
                false
            );

            protected override IEnumerable<MemberDeclarationSyntax> BuildMembers(CancellationToken cancellation) => Context.InterfaceType
                .Properties
                .Where(prop => !AlreadyImplemented(prop))
                .Select(prop => 
                {
                    cancellation.ThrowIfCancellationRequested();

                    return BuildProperty
                    (
                        prop,
                        prop.Indices.Any()
                            ? DeclareIndexer
                            : DeclareProperty
                    );
                });

            public PropertyInterceptorFactory(IProxyContext context) : base(context) 
            {
                RESOLVE_PROPERTY = Context.InterceptorType.Methods.Single
                (
                    met => met.SignatureEquals
                    (
                        MetadataMethodInfo.CreateFrom
                        (
                            (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<object>.ResolveProperty(default!))
                        )
                    )
                );
            }
        }
    }
}