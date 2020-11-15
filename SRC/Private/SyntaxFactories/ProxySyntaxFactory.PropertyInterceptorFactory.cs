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
        internal class PropertyInterceptorFactory : InterceptorFactoryBase
        {
            private static readonly MethodInfo
                RESOLVE_PROPERTY = (MethodInfo) MemberInfoExtensions.ExtractFrom(() => InterfaceInterceptor<TInterface>.ResolveProperty(default!));

            protected static IEnumerable<StatementSyntax> BuildGet(PropertyInfo property) 
            {
                if (!property.CanRead) yield break;

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(property.GetMethod);
                yield return argsArray;

                yield return AssignCallback
                (
                    DeclareCallback
                    (
                        argsArray,
                        property.GetMethod,
                        (locals, result) => new StatementSyntax[] 
                        {
                            ExpressionStatement
                            (
                                AssignmentExpression
                                (
                                    SyntaxKind.SimpleAssignmentExpression,
                                    ToIdentifierName(result!),
                                    CastExpression
                                    (
                                        CreateType<object>(),
                                        PropertyAccess(property, TARGET, null, locals.Select(arg => Argument(ToIdentifierName(arg))))
                                    )
                                )
                            )
                        }
                    )
                );

                LocalDeclarationStatementSyntax prop = DeclareLocal(typeof(PropertyInfo), EnsureUnused(nameof(prop), property.GetMethod), InvokeMethod
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
                    property.PropertyType,
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

            protected static IEnumerable<StatementSyntax> BuildSet(PropertyInfo property)
            {
                if (!property.CanWrite) yield break;

                LocalDeclarationStatementSyntax argsArray = CreateArgumentsArray(property.SetMethod);
                yield return argsArray;

                yield return AssignCallback
                (
                    DeclareCallback
                    (
                        argsArray,
                        property.SetMethod,
                        (locals, result) => new StatementSyntax[]
                        {
                            ExpressionStatement
                            (
                                expression: AssignmentExpression
                                (
                                    kind: SyntaxKind.SimpleAssignmentExpression,
                                    left: PropertyAccess(property, TARGET, null, locals
#if NETSTANDARD2_0
                                        .Take(locals.Count - 1)
#else
                                        .SkipLast(1)
#endif
                                        .Select(arg => Argument(ToIdentifierName(arg)))),
                                    right: ToIdentifierName(locals[locals.Count - 1])
                                )
                            )
                        }
                    )
                );

                LocalDeclarationStatementSyntax prop = DeclareLocal(typeof(PropertyInfo), EnsureUnused(nameof(prop), property.SetMethod), InvokeMethod
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

            public override bool IsCompatible(MemberInfo member) => member is PropertyInfo prop && prop.DeclaringType.IsInterface && !prop.IsIndexer() && !AlreadyImplemented(prop);

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            public override MemberDeclarationSyntax Build(MemberInfo member)
            {
                PropertyInfo property = (PropertyInfo) member;

                return DeclareProperty
                (
                    property: property,
                    getBody: Block
                    (
                        BuildGet(property)
                    ),
                    setBody: Block
                    (
                        BuildSet(property)
                    )
                );
            }
        }
    }
}