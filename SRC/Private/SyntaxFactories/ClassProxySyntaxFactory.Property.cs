/********************************************************************************
* ClassProxySyntaxFactory.Property.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassProxySyntaxFactory
    {
        /// <summary>
        /// <code>
        /// private static ExtendedMemberInfo FXxX;
        /// public override T Prop
        /// {
        ///     get
        ///     {
        ///         CurrentMethod.GetBase(ref FXxX);
        ///     
        ///         object[] args = new object[] {value};
        ///         
        ///         return (T) FInterceptor.Invoke
        ///         (
        ///             new ClassInvocationContext
        ///             (
        ///                 FXxX,
        ///                 args => base.Prop;
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
        protected override ClassDeclarationSyntax ResolveProperty(ClassDeclarationSyntax cls, object context, IPropertyInfo prop)
        {
            List<MemberDeclarationSyntax> members = new();

            BlockSyntax?
                get = BuildBody
                (
                    prop.GetMethod,
                    (_, locals) => PropertyAccess
                    (
                        prop,
                        BaseExpression(),
                        castTargetTo: null,
                        indices: locals.Select(ResolveArgument)
                    )
                ),
                set = BuildBody
                (
                    prop.SetMethod,
                    (_, locals) => AssignmentExpression // target.Prop = _value
                    (
                        kind: SyntaxKind.SimpleAssignmentExpression,
                        left: PropertyAccess
                        (
                            prop,
                            BaseExpression(),
                            castTargetTo: null,
                            indices: locals
                                .Take(locals.Count - 1)
                                .Select(ResolveArgument)
                        ),
                        right: ResolveIdentifierName(locals.Last())
                    )
                );

            BlockSyntax? BuildBody(IMethodInfo? backingMethod, Func<IReadOnlyList<ParameterSyntax>, IReadOnlyList<LocalDeclarationStatementSyntax>, ExpressionSyntax> invocationFactory)
            {
                if (backingMethod is null)
                    return null;

                FieldDeclarationSyntax field = ResolveField<ExtendedMemberInfo>
                (
                    $"F{backingMethod.GetMD5HashCode()}",
                    @readonly: false
                );

                members.Add(field);

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(backingMethod);

                InvocationExpressionSyntax interceptorInvocation = InvokeMethod
                (
                    method: FInvoke,
                    target: MemberAccess
                    (
                        ResolveIdentifierName(FInterceptor),
                        FInvoke
                    ),
                    castTargetTo: null,
                    arguments: Argument
                    (
                        ResolveObject<ClassInvocationContext>
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
                                    invocationFactory
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
                    )
                );

                return Block
                (
                    argsArray,
                    backingMethod.ReturnValue.Type.IsVoid
                        ? ExpressionStatement(interceptorInvocation)
                        : ReturnResult(backingMethod.ReturnValue.Type, interceptorInvocation)
                );
            }

            members.Add
            (
                prop.Indices.Any()
                    ? ResolveIndexer(prop, get, set, false)
                    : ResolveProperty(prop, get, set, false)
            );

            return cls.AddMembers(members.ToArray());
        }
    }
}