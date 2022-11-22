/********************************************************************************
* ProxyUnitSyntaxFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal abstract class ProxyUnitSyntaxFactory : UnitSyntaxFactoryBase
    {
        protected ProxyUnitSyntaxFactory(OutputType outputType, string containingAssembly, ReferenceCollector? referenceCollector): base(outputType, referenceCollector) =>
            ContainingAssembly = containingAssembly;

        public string ContainingAssembly { get; }

        //
        // Proxy egyseg mindig csak egy osztalyt definial
        //

        public override IReadOnlyCollection<string> DefinedClasses => new string[]
        {
            ResolveClassName(null!)
        };

        protected override IEnumerable<Func<ClassDeclarationSyntax, object, ClassDeclarationSyntax>> MemberResolvers
        {
            get
            {
                foreach (Func<ClassDeclarationSyntax, object, ClassDeclarationSyntax> resolver in base.MemberResolvers)
                {
                    yield return resolver;
                }
                yield return ResolveActivator;
                yield return ResolveInitializer;
            }
        }

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveInitializer(ClassDeclarationSyntax cls, object context) => cls.AddMembers
        (
            //
            // [ModuleInitializerAttribute]
            // public static void Initialize() => RegisterInstance(typeof(CurrentClass)); // C# 7.0 compatible
            //

            MethodDeclaration
            (
                ResolveType
                (
                    MetadataTypeInfo.CreateFrom(typeof(void))
                ),
                Identifier("Initialize")
            )
            .WithModifiers
            (
                TokenList
                (
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword)
                )
            )
            .WithAttributeLists
            (
                SingletonList
                (
                    AttributeList
                    (
                        SingletonSeparatedList
                        (
                            ResolveAttribute(typeof(ModuleInitializerAttribute))
                        )
                    )
                )
            )
            .WithExpressionBody
            (
                ArrowExpressionClause
                (
                    InvokeMethod
                    (
                        MetadataMethodInfo.CreateFrom
                        (
                            (MethodInfo) MemberInfoExtensions.ExtractFrom(() => GeneratedClass.RegisterInstance(null!))
                        ),
                        target: null,
                        castTargetTo: null,
                        Argument
                        (
                            TypeOfExpression
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
                        )
                    )
                )
            )
            .WithSemicolonToken
            (
                Token(SyntaxKind.SemicolonToken)
            )
        );

        public const string ACTIVATOR_NAME = "__Activator";

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveActivator(ClassDeclarationSyntax cls, object context)
        {
            //
            // public static readonly Func<object, object> __Activator = tuple =>
            // {
            //    switch (tuple)
            //    {
            //       case null: return new Class();
            //       case Tuple<int, string> t1: return new Class(t1.Item1, t1.Item2);  // C# 7.0 compatible
            //       default: throw new NotSupportedException();
            //    }
            // }
            //

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
                                ResolveType<NotSupportedException>()
                            )
                            .WithArgumentList
                            (
                                ArgumentList()
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
                        SingletonList<StatementSyntax>
                        (
                            ReturnStatement
                            (
                                ObjectCreationExpression
                                (
                                    IdentifierName(cls.Identifier)
                                )
                                .WithArgumentList
                                (
                                    ArgumentList()
                                )
                            )
                        )
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
                    SingletonList<StatementSyntax>
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
                                ArgumentList
                                (
                                    ctor.ParameterList.Parameters.Count.Times
                                    (
                                        i => Argument
                                        (
                                            MemberAccessExpression
                                            (
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName(tupleId),
                                                IdentifierName($"Item{i + 1}")
                                            )
                                        )
                                    )
                                    .ToSyntaxList()
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