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
            foreach (IPropertyInfo prop in FBaseType.Properties)
            {
                IMethodInfo targetMethod = prop.GetMethod ?? prop.SetMethod!;
                if (targetMethod.IsAbstract || targetMethod.IsVirtual)
                    cls = ResolveProperty(cls, context, prop);
            }

            return base.ResolveProperties(cls, context);
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
        ///             new InvocationContext
        ///             (
        ///                 this,
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
            List<MemberDeclarationSyntax> members = [];

            BlockSyntax? get;
            if (prop.GetMethod is null) get = null; else
            {
                if (!IsVisible(prop.GetMethod))
                    return cls;

                get = BuildBody
                (
                    prop.GetMethod,
                    (_, locals) => PropertyAccess
                    (
                        prop,
                        BaseExpression(),
                        indices: locals.Select(ResolveArgument)
                    )
                );
            }

            BlockSyntax? set;
            if (prop.SetMethod is null) set = null; else
            {
                if (!IsVisible(prop.SetMethod))
                    return cls;

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
                            indices: locals
                                .Take(locals.Count - 1)
                                .Select(ResolveArgument)
                        ),
                        right: ResolveIdentifierName(locals.Last())
                    )
                );
            }

            members.Add
            (
                prop.Indices.Any()
                    ? ResolveIndexer(prop, get, set)
                    : ResolveProperty(prop, get, set)
            );

            return cls.AddMembers([.. members]);

            BlockSyntax? BuildBody(IMethodInfo backingMethod, Func<IReadOnlyList<ParameterSyntax>, IReadOnlyList<LocalDeclarationStatementSyntax>, ExpressionSyntax> invocationFactory)
            {
                FieldDeclarationSyntax field = ResolveField<ExtendedMemberInfo>
                (
                    $"F{backingMethod.GetMD5HashCode()}",
                    @readonly: false
                );

                members.Add(field);

                return Block
                (
                    (StatementSyntax[])
                    [
                        ExpressionStatement
                        (
                            InvokeMethod
                            (
                                FGetBase,
                                arguments: Argument
                                (
                                    StaticMemberAccess(cls, field)
                                )
                            )
                        ),
                        ..ResolveInvokeInterceptor<InvocationContext>
                        (
                            backingMethod,
                            argsArray =>
                            [
                                Argument
                                (
                                    ThisExpression()
                                ),
                                Argument
                                (
                                    StaticMemberAccess(cls, field)
                                ),
                                Argument
                                (
                                    backingMethod.IsAbstract ? ResolveNotImplemented() : ResolveInvokeTarget(backingMethod, invocationFactory)
                                ),
                                Argument
                                (
                                    ResolveIdentifierName(argsArray)
                                ),
                                Argument
                                (
                                    ResolveArray<Type>([])
                                )
                            ]
                        )
                    ]
                );
            }
        }
    }
}