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
        public const string ACTIVATOR_NAME = "__Activator";

        /// <summary>
        /// <code>
        /// public static readonly Func&lt;object, object&gt; __Activator = static tuple =>
        /// {
        ///    switch (tuple)
        ///    {
        ///        case null: return new Class();
        ///        case Tuple&lt;int, string&gt; t1: return new Class(t1.Item1, t1.Item2);  // C# 7.0 compatible
        ///        default: throw new MissingMethodException("...");
        ///    }
        /// }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveActivator(ClassDeclarationSyntax cls, object context)
        {
            const string tuple = nameof(tuple);

            return cls.AddMembers
            (
                FieldDeclaration
                (
                    VariableDeclaration
                    (
                        ResolveType<Func<object, object>>()
                    )
                    .WithVariables
                    (
                        SingletonSeparatedList
                        (
                            VariableDeclarator
                            (
                                Identifier(ACTIVATOR_NAME)
                            )
                            .WithInitializer
                            (
                                EqualsValueClause
                                (
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
                                            SingletonList<StatementSyntax>
                                            (
                                                SwitchStatement
                                                (
                                                    IdentifierName(tuple)
                                                )
                                                .WithSections
                                                (
                                                    List
                                                    (
                                                        GetCases()
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
                .WithModifiers
                (
                    TokenList
                    (
                        new[]
                        {
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.StaticKeyword),
                            Token(SyntaxKind.ReadOnlyKeyword)
                        }
                    )
                )        
            );

            IEnumerable<SwitchSectionSyntax> GetCases()
            {
                int i = 0;
                foreach (ConstructorDeclarationSyntax ctor in cls.Members.OfType<ConstructorDeclarationSyntax>())
                {
                    //
                    // Tuple may hold at most 7 items
                    //

                    if (ctor.ParameterList.Parameters.Count <= 7)
                        yield return GetCase(ctor, ref i);
                }

                yield return SwitchSection().WithLabels
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

            SwitchSectionSyntax GetCase(ConstructorDeclarationSyntax ctor, ref int i)
            {
                if (ctor.ParameterList.Parameters.Count is 0)
                {
                    return SwitchSection().WithLabels
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
                        InvokeCtor()
                    );
                }

                string tupleId = $"t{i++}";

                return SwitchSection().WithLabels
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
                    InvokeCtor
                    (
                        ctor.ParameterList.Parameters.Count.Times
                        (
                            i => Argument
                            (
                                SimpleMemberAccess
                                (
                                    IdentifierName(tupleId),
                                    IdentifierName($"Item{i + 1}")
                                )
                            )
                        )
                    )
                );
            }

            SyntaxList<StatementSyntax> InvokeCtor(params IEnumerable<ArgumentSyntax> arguments) => SingletonList<StatementSyntax>
            (
                ReturnStatement
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
                        ArgumentList(arguments.ToSyntaxList())
                    )
                )
            );

            TypeSyntax GetTupleForCtor(ConstructorDeclarationSyntax ctor)
            {
                TypeSyntax generic = ResolveType
                (
                    MetadataTypeInfo.CreateFrom
                    (
                        typeof(Tuple)
                            .Assembly
                            .GetType($"System.Tuple`{ctor.ParameterList.Parameters.Count}", throwOnError: true)
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