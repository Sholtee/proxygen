/********************************************************************************
* ClassProxySyntaxFactory.PropertyInterceptorFactory.cs                         *
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
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context)
        {
            foreach (IPropertyInfo prop in TargetType.Properties)
            {
                IMethodInfo targetMethod = prop.GetMethod ?? prop.SetMethod!;
                if (targetMethod.IsAbstract || targetMethod.IsVirtual)
                    cls = ResolveProperty(cls, context, prop);
            }

            return cls.AddMembers
            (
                ResolveProperty(FInterceptor, null, null)
            );
        }

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
                    (_, locals) => AssignmentExpression // base.Prop = _value
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

                //
                // Check if the method is visible.
                //

                Visibility.Check(backingMethod, ContainingAssembly, allowProtected: true);

                FieldDeclarationSyntax field = ResolveField<ExtendedMemberInfo>
                (
                    $"F{backingMethod.GetMD5HashCode()}",
                    @readonly: false
                );

                members.Add(field);

                LocalDeclarationStatementSyntax argsArray = ResolveArgumentsArray(backingMethod);

                InvocationExpressionSyntax interceptorInvocation = InvokeInterceptor
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
                    ? ResolveIndexer(prop, get, set)
                    : ResolveProperty(prop, get, set)
            );

            return cls.AddMembers(members.ToArray());
        }
    }
}