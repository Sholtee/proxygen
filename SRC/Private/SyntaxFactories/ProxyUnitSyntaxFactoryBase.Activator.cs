/********************************************************************************
* ProxyUnitSyntaxFactoryBase.Activator.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal abstract partial class ProxyUnitSyntaxFactoryBase
    {
        protected const string ACTIVATOR_NAME = "__Activator";

        /// <summary>
        /// <code>
        /// public static readonly Func&lt;object, object&gt; __Activator = static tuple =>
        /// {
        ///    switch (tuple)
        ///    {
        ///        case null: return new Class();
        ///        case Tuple&lt;int, string&gt; t1: return new Class(t1.Item1, t1.Item2);
        ///        default: throw new MissingMethodException("...");
        ///    }
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected ClassDeclarationSyntax ResolveActivator(ClassDeclarationSyntax cls, object context)
        {
            const string tuple = nameof(tuple);

            return cls.AddMembers
            (
                ResolveField
                (
                    MetadataTypeInfo.CreateFrom(typeof(Func<object, object>)),
                    ACTIVATOR_NAME,
                    initializer:
                        SimpleLambdaExpression
                        (
                            Parameter
                            (
                                Identifier(tuple)
                            )
                        )
                        .WithModifiers
                        (
                            TokenList
                            (
                                Token(SyntaxKind.StaticKeyword)
                            )
                        )
                        .WithBlock
                        (
                            Block
                            (
                                List
                                (
                                    ResolveActivatorBody
                                    (
                                        cls,
                                        IdentifierName(tuple),
                                        context
                                    )
                                )
                            )
                        ),
                    @private: false
                )
            );
        }

        /// <summary>
        /// <code>
        /// new Proxy(...);
        /// </code>
        /// </summary>
        protected virtual IEnumerable<StatementSyntax> ResolveProxyObject(ClassDeclarationSyntax cls, object context, params IEnumerable<ExpressionSyntax> arguments)
        {
            yield return ReturnStatement
            (
                ObjectCreationExpression
                (
                    AliasQualifiedName
                    (
                        IdentifierName
                        (
                            Token(SyntaxKind.GlobalKeyword)
                        ),
                        IdentifierName(cls.Identifier)
                    )
                )
                .WithArgumentList
                (
                    ArgumentList
                    (
                        arguments
                            .Select(Argument)
                            .ToSyntaxList()
                    )
                )
            );
        }

        /// <summary>
        /// <code>
        /// t1.Item1, t1.Item2
        /// </code>
        /// </summary>
        protected virtual IEnumerable<ExpressionSyntax> ResolveProxyObjectParameters(ConstructorDeclarationSyntax ctor, string tupleId, object context) => ctor.ParameterList.Parameters.Select
        (
            (parameter, i) =>
            {
                if (parameter.Modifiers.Any(static token => token.IsKind(SyntaxKind.OutKeyword) || token.IsKind(SyntaxKind.RefKeyword)))
                    throw new InvalidOperationException(Resources.BYREF_CTOR_PARAMETER);

                return SimpleMemberAccess
                (
                    IdentifierName(tupleId),
                    IdentifierName($"Item{i + 1}")
                );
            }
        );

        /// <summary>
        /// <code>
        /// switch (tuple)
        /// {
        ///     case null: return new Class();
        ///     case Tuple&lt;int, string&gt; t1: return new Class(t1.Item1, t1.Item2);
        ///     default: throw new MissingMethodException("...");
        /// }
        /// </code>
        /// </summary>
        protected virtual IEnumerable<StatementSyntax> ResolveActivatorBody(ClassDeclarationSyntax cls, ExpressionSyntax tuple, object context)
        {
            yield return SwitchStatement(tuple).WithSections
            (
                List
                (
                    GetCases()
                )
            );

            IEnumerable<SwitchSectionSyntax> GetCases()
            {
                int i = 0;
                foreach (ConstructorDeclarationSyntax ctor in cls.Members.OfType<ConstructorDeclarationSyntax>())
                {
                    string tupleId = $"t{i}";
                    IReadOnlyList<ExpressionSyntax> parameters = [.. ResolveProxyObjectParameters(ctor, tupleId, context)];

                    switch (parameters.Count)
                    {
                        case > 7:
                            //
                            // Tuple may hold at most 7 items
                            //

                            throw new InvalidOperationException(Resources.TOO_MANY_CTOR_PARAMS);
                        case 0:
                            yield return SwitchSection()
                                .WithLabels
                                (
                                    SingletonList<SwitchLabelSyntax>
                                    (
                                        CaseSwitchLabel
                                        (
                                            LiteralExpression(SyntaxKind.NullLiteralExpression)
                                        )
                                    )
                                )
                                .WithStatements
                                (
                                    List
                                    (
                                        ResolveProxyObject(cls, context)
                                    )
                                );
                            break;
                        default:
                            i++;

                            yield return SwitchSection()
                                .WithLabels
                                (
                                    SingletonList<SwitchLabelSyntax>
                                    (
                                        CasePatternSwitchLabel
                                        (
                                            DeclarationPattern
                                            (
                                                GetTupleForCtor(ctor),
                                                SingleVariableDesignation
                                                (
                                                    Identifier(tupleId)
                                                )
                                            ),
                                            Token(SyntaxKind.ColonToken)
                                        )
                                    )
                                )
                                .WithStatements
                                (
                                    List
                                    (
                                        ResolveProxyObject(cls, context, parameters)
                                    )
                                );
                            break;
                    }
                }

                yield return SwitchSection()
                    .WithLabels
                    (
                        SingletonList<SwitchLabelSyntax>
                        (
                            DefaultSwitchLabel()
                        )
                    )
                    .WithStatements
                    (
                        SingletonList<StatementSyntax>
                        (
                            ThrowStatement
                            (
                                ObjectCreationExpression
                                (
                                    ResolveType<MissingMethodException>()
                                )
                                .WithArgumentList
                                (
                                    ArgumentList
                                    (
                                        SingletonSeparatedList
                                        (
                                            Argument
                                            (
                                                Resources.CTOR_NOT_FOUND.AsLiteral()
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    );
            }

            TypeSyntax GetTupleForCtor(ConstructorDeclarationSyntax ctor)
            {
                TypeSyntax generic = ResolveType
                (
                    MetadataTypeInfo.CreateFrom
                    (
                        typeof(Tuple).Assembly.GetType
                        (
                            $"System.Tuple`{ctor.ParameterList.Parameters.Count}",
                            throwOnError: true
                        )
                    )
                );

                //
                // Hacky specialization
                //

                int arity = 0;

                SyntaxNode gn = generic
                    .ChildNodes()
                    .Single(static node => node is GenericNameSyntax)!;

                return generic.ReplaceNodes
                (
                    gn.DescendantNodes().OfType<TypeSyntax>(),
                    (_, _) => ctor.ParameterList.Parameters[arity++].Type!
                );
            }
        }
    }
}