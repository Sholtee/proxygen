/********************************************************************************
* InterfaceProxySyntaxFactory.PropertyInterceptorFactory.cs                     *
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
    internal partial class InterfaceProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveProperties(ClassDeclarationSyntax cls, object context)
        {
            foreach (IPropertyInfo prop in TargetType!.Properties)
                cls = ResolveProperty(cls, context, prop);

            return cls;
        }

        /// <summary>
        /// <code>
        /// private static ExtendedMemberInfo FXxX;
        /// T Interface.Prop
        /// {
        ///     get
        ///     {
        ///         CurrentMethod.GetImplementedInterfaceMethod(ref FXxX);
        ///     
        ///         object[] args = new object[] {value};
        ///         
        ///         return (T) FInterceptor.Invoke
        ///         (
        ///             new InvocationContext
        ///             (
        ///                 this,
        ///                 FXxX,
        ///                 args => FTarget.Prop;
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

            BlockSyntax?
                get = null,
                set = null;

            if (prop.GetMethod is not null)
            {
                //
                // Starting from .NET 5.0 interface members may have visibility.
                //

                Visibility.Check(prop.GetMethod, ContainingAssembly);

                get = BuildBody
                (
                    prop.GetMethod,
                    (_, locals) => PropertyAccess
                    (
                        prop,
                        GetTarget(),
                        indices: locals.Select(ResolveArgument)
                    )
                );
            }

            if (prop.SetMethod is not null)
            {
                Visibility.Check(prop.SetMethod, ContainingAssembly);

                set = BuildBody
                (
                    prop.SetMethod,
                    (_, locals) => AssignmentExpression // FTarget.Prop = _value
                    (
                        kind: SyntaxKind.SimpleAssignmentExpression,
                        left: PropertyAccess
                        (
                            prop,
                            GetTarget(),
                            indices: locals
                                .Take(locals.Count - 1)
                                .Select(ResolveArgument)
                        ),
                        right: ResolveIdentifierName(locals.Last())  // "value" always the last parameter
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
                                FGetImplementedInterfaceMethod,
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
                                    ResolveInvokeTarget(backingMethod, invocationFactory)
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