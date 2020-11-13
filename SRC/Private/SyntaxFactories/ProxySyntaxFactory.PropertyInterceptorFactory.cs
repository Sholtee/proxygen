/********************************************************************************
* ProxySyntaxFactory.PropertyInterceptorFactory.cs                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory<TInterface, TInterceptor> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        /// <summary>
        /// TResult IInterface.Prop                                                          <br/>
        /// {                                                                                <br/>
        ///     get                                                                          <br/>
        ///     {                                                                            <br/>
        ///         object[] args = new object[] { };                                        <br/>
        ///         InvokeTarget = () =>                                                     <br/>
        ///         {                                                                        <br/>
        ///             return Target.Prop;                                                  <br/>
        ///         };                                                                       <br/>
        ///         PropertyInfo prop = ResolveProperty(InvokeTarget);                       <br/>
        ///         return (TResult) Invoke(prop.GetMethod, args, prop);                     <br/>
        ///     }                                                                            <br/>
        ///     set                                                                          <br/>
        ///     {                                                                            <br/>
        ///         object[] args = new object[] {value};                                    <br/>
        ///         InvokeTarget = () =>                                                     <br/>
        ///         {                                                                        <br/>
        ///           TResult cb_value = (TResult) args[0];                                  <br/>
        ///           Target.Prop = cb_value;                                                <br/>
        ///           return null;                                                           <br/>
        ///         };                                                                       <br/>
        ///         PropertyInfo prop = ResolveProperty(InvokeTarget);                       <br/>
        ///         Invoke(prop.SetMethod, args, prop);                                      <br/>
        ///     }                                                                            <br/>
        /// }
        /// </summary>
        internal class PropertyInterceptorFactory : IInterceptorFactory
        {
            private static readonly MethodInfo
                RESOLVE_PROPERTY = (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.ResolveProperty(default!));

            public PropertyInfo Property { get; }

            public PropertyInterceptorFactory(PropertyInfo property) => Property = property;

            internal IEnumerable<StatementSyntax> BuildGet() 
            {
                if (!Property.CanRead) yield break;

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(Property.GetMethod);
                yield return argsArray;

                yield return AssignCallback
                (
                    DeclareCallback
                    (
                        argsArray,
                        Property.GetMethod.GetParameters(),
                        (locals, result) => new StatementSyntax[] 
                        {
                            ExpressionStatement
                            (
                                AssignmentExpression
                                (
                                    SyntaxKind.SimpleAssignmentExpression,
                                    ToIdentifierName(result),
                                    PropertyAccess(Property, TARGET, null, locals.Select(arg => Argument(ToIdentifierName(arg))))
                                )
                            )
                        }
                    )
                );

                LocalDeclarationStatementSyntax prop = DeclareLocal(typeof(PropertyInfo), EnsureUnused(nameof(prop), Property.GetMethod.GetParameters()), InvokeMethod
                (
                    RESOLVE_PROPERTY,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        expression: PropertyAccess(INVOKE_TARGET, null, null)
                    )
                ));

                yield return prop;

                yield return ReturnResult
                (
                    Property.PropertyType,
                    InvokeMethod
                    (
                        INVOKE,
                        target: null,
                        castTargetTo: null,
                        arguments: new ExpressionSyntax[]
                        {
                            SimpleMemberAccess // prop.GetMethod
                            (
                                ToIdentifierName(prop),  
                                nameof(PropertyInfo.GetMethod)
                            ),
                            ToIdentifierName(argsArray), // new object[0] | new object[] {index1, index2, ...}       
                            ToIdentifierName(prop) // prop
                        }.Select(Argument).ToArray()
                    )
                );
            }

            internal IEnumerable<StatementSyntax> BuildSet()
            {
                if (!Property.CanWrite) yield break;

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(Property.SetMethod);
                yield return argsArray;

                yield return AssignCallback
                (
                    DeclareCallback
                    (
                        argsArray,
                        Property.SetMethod.GetParameters(),
                        (locals, result) => new StatementSyntax[]
                        {
                            ExpressionStatement
                            (
                                expression: AssignmentExpression
                                (
                                    kind: SyntaxKind.SimpleAssignmentExpression,
                                    left: PropertyAccess(Property, TARGET, null, locals
#if NETSTANDARD2_0
                                        .Take(locals.Count - 1)
#else
                                        .SkipLast(1)
#endif
                                        .Select(arg => Argument(ToIdentifierName(arg)))),
                                    right: ToIdentifierName(locals[locals.Count - 1])
                                )
                            ),
                            ExpressionStatement
                            (
                                AssignmentExpression
                                (
                                    SyntaxKind.SimpleAssignmentExpression,
                                    ToIdentifierName(result),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)
                                )
                            )
                        }
                    )
                );

                LocalDeclarationStatementSyntax prop = DeclareLocal(typeof(PropertyInfo), EnsureUnused(nameof(prop), Property.SetMethod.GetParameters()), InvokeMethod
                (
                    RESOLVE_PROPERTY,
                    target: null,
                    castTargetTo: null,
                    Argument
                    (
                        expression: PropertyAccess(INVOKE_TARGET, null, null)
                    )
                ));

                yield return prop;

                yield return ExpressionStatement
                (
                    InvokeMethod
                    (
                        INVOKE,
                        target: null,
                        castTargetTo: null,
                        arguments: new ExpressionSyntax[]
                        {
                            SimpleMemberAccess // prop.SetMethod
                            (
                                ToIdentifierName(prop),
                                nameof(PropertyInfo.SetMethod)
                            ),
                            ToIdentifierName(argsArray), //  new object[] {value} | new object[] {index1, index2, ..., value}
                            ToIdentifierName(prop) // prop
                        }.Select(Argument).ToArray()
                    )
                );
            }

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            protected virtual MemberDeclarationSyntax DeclareProperty() => ProxySyntaxFactoryBase.DeclareProperty
            (
                property: Property,
                getBody: Block
                (
                    BuildGet()
                ),
                setBody: Block
                (
                    BuildSet()
                )
            );

            public MemberDeclarationSyntax Build() => DeclareProperty();
        }
    }
}