/********************************************************************************
* ProxySyntaxFactoryBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Abstractions;

    internal abstract class ProxySyntaxFactoryBase: ISyntaxFactory
    {
        // https://github.com/dotnet/roslyn/issues/4861
        protected const string Value = "value";

        #region Private
        private static SyntaxList<AttributeListSyntax> DeclareMethodImplAttributeToForceInlining() => SingletonList
        (
            node: AttributeList
            (
                attributes: SingletonSeparatedList
                (
                    node: Attribute
                    (
                        (NameSyntax) CreateType<MethodImplAttribute>()
                    )
                    .WithArgumentList
                    (
                        argumentList: AttributeArgumentList
                        (
                            arguments: SingletonSeparatedList
                            (
                                node: AttributeArgument
                                (
                                    MemberAccessExpression
                                    (
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        CreateType<MethodImplOptions>(),
                                        IdentifierName(nameof(MethodImplOptions.AggressiveInlining))
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );

        private static AccessorDeclarationSyntax DeclareAccessor(SyntaxKind kind, CSharpSyntaxNode body, bool forceInlining)
        {
            AccessorDeclarationSyntax declaration = AccessorDeclaration(kind);

            switch (body)
            {
                case BlockSyntax block:
                    declaration = declaration.WithBody(block);
                    break;
                case ArrowExpressionClauseSyntax arrow:
                    declaration = declaration
                        .WithExpressionBody(arrow)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                    break;
                default:
                    Debug.Fail("Unknown node type");
                    return null!;
            }

            if (forceInlining) declaration = declaration.WithAttributeLists
            (
                attributeLists: DeclareMethodImplAttributeToForceInlining()
            );

            return declaration;
        }

        private static ExpressionSyntax AmendTarget(ExpressionSyntax? target, MemberInfo member, Type? castTargetTo)
        {
            target ??= member.IsStatic() ? CreateType(member.DeclaringType) : (ExpressionSyntax) ThisExpression();

            if (castTargetTo != null) 
            {
                Debug.Assert(!member.IsStatic());

                target = ParenthesizedExpression
                (
                    CastExpression(CreateType(castTargetTo), target)
                );
            }

            return target;
        }
        #endregion

        #region Protected
        protected internal static string GetSafeTypeName<T>() => CreateType<T>()
            .ToFullString()
            //
            // Csak karaktert es ne karakterlancot csereljunk h az eredmenyt ne befolyasolja a
            // felhasznalo teruleti beallitasa.
            //
            .Replace(',', '_');

        protected internal static MemberAccessExpressionSyntax SimpleMemberAccess(ExpressionSyntax target, string member) =>
            MemberAccessExpression
            (
                SyntaxKind.SimpleMemberAccessExpression,
                target,
                IdentifierName(member)
            );

        /// <summary>
        /// [[(Type)] target | [(Type)] this | Namespace.Type].Member
        /// </summary>
        protected internal static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax? target, MemberInfo member, Type? castTargetTo = null) =>
            MemberAccessExpression
            (
                SyntaxKind.SimpleMemberAccessExpression,
                AmendTarget(target, member, castTargetTo),
                IdentifierName(member.StrippedName())
            );

        /// <summary>
        /// [[(Type)] target | [(Type)] this | Namespace.Type].Method[...](...)
        /// </summary>
        protected internal static MemberAccessExpressionSyntax MethodAccess(ExpressionSyntax? target, MethodInfo method, Type? castTargetTo = null) 
        {
            string methodName = method.StrippedName();

            SimpleNameSyntax name = !method.IsGenericMethod
                ? (SimpleNameSyntax) IdentifierName(methodName)
                : (SimpleNameSyntax) GenericName(Identifier(methodName)).WithTypeArgumentList
                (
                    typeArgumentList: TypeArgumentList
                    (
                        arguments: method.GetGenericArguments().ToSyntaxList(CreateType)
                    )                  
                );

            return MemberAccessExpression
            (
                SyntaxKind.SimpleMemberAccessExpression,
                AmendTarget(target, method, castTargetTo),
                name
            );
        }

        /// <summary>
        /// [target | this | Namespace.Type].Prop[...]
        /// </summary>
        protected internal static ElementAccessExpressionSyntax ElementAccess(ExpressionSyntax? target, MemberInfo prop) =>
            ElementAccessExpression
            (
                MemberAccess(target, prop)
            );

        /// <summary>
        /// System.Object paramName [= ...];
        /// </summary>
        protected internal static LocalDeclarationStatementSyntax DeclareLocal(Type type, string name, ExpressionSyntax? initializer = null)
        {
            VariableDeclaratorSyntax declarator = VariableDeclarator
            (
                identifier: Identifier(name)
            );

            if (initializer != null) declarator = declarator.WithInitializer
            (
                initializer: EqualsValueClause(initializer)
            );

            return LocalDeclarationStatement
            (
                declaration: VariableDeclaration
                (
                    type: CreateType(type),
                    variables: SeparatedList(new List<VariableDeclaratorSyntax>
                    {
                        declarator
                    })
                )
            );
        }

        /// <summary>
        /// System.Object paramName [= ...];
        /// </summary>
        protected internal static LocalDeclarationStatementSyntax DeclareLocal<T>(string name, ExpressionSyntax? initializer = null) => DeclareLocal(typeof(T), name, initializer);

        /// <summary>
        /// new System.Object[] {..., ..., ...}
        /// </summary>
        protected internal static ArrayCreationExpressionSyntax CreateArray<T>(params ExpressionSyntax[] elements) => ArrayCreationExpression
        (
            type: ArrayType
            (
                elementType: CreateType<T>()
            )
            .WithRankSpecifiers
            (
                rankSpecifiers: SingletonList
                (
                    ArrayRankSpecifier(SingletonSeparatedList
                    (
                        elements.Any() ? OmittedArraySizeExpression() : (ExpressionSyntax) LiteralExpression
                        (
                            SyntaxKind.NumericLiteralExpression,
                            Literal(0)
                        )
                    ))
                )
            ),
            initializer: !elements.Any() ? null : InitializerExpression(SyntaxKind.ArrayInitializerExpression).WithExpressions
            (
                expressions: elements.ToSyntaxList()
            )
        );

        /// <summary>
        /// int IInterface.Foo[T](string a, ref T b)
        /// </summary>
        protected internal static MethodDeclarationSyntax DeclareMethod(MethodInfo method, bool forceInlining = false)
        {
            Type 
                declaringType = method.DeclaringType,
                returnType    = method.ReturnType;

            Debug.Assert(declaringType.IsInterface);

            TypeSyntax returnTypeSytax = CreateType(returnType);

            if (returnType.IsByRef) 
                returnTypeSytax = RefType(returnTypeSytax);

            MethodDeclarationSyntax result = MethodDeclaration
            (
                returnType: returnTypeSytax,
                identifier: Identifier(method.StrippedName())
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(declaringType))
            )
            .WithParameterList
            (
                ParameterList
                (
                    parameters: method.GetParameters().ToSyntaxList(param =>
                    {
                        ParameterSyntax parameter = Parameter(Identifier(param.Name)).WithType
                        (
                            type: CreateType(param.ParameterType)
                        );

                        List<SyntaxKind> modifiers = new List<SyntaxKind>();

                        switch (param.GetParameterKind())
                        {
                            case ParameterKind.In:
                                modifiers.Add(SyntaxKind.InKeyword);
                                break;
                            case ParameterKind.Out:
                                modifiers.Add(SyntaxKind.OutKeyword);
                                break;
                            case ParameterKind.InOut:
                                modifiers.Add(SyntaxKind.RefKeyword);
                                break;
                            case ParameterKind.Params:
                                modifiers.Add(SyntaxKind.ParamsKeyword);
                                break;
                        }

                        if (modifiers.Any()) 
                            parameter = parameter.WithModifiers(TokenList(modifiers.Select(Token)));
     
                        return parameter;
                    })
                )
            );

            if (method.IsGenericMethod) result = result.WithTypeParameterList // kulon legyen kulomben lesz egy ures "<>"
            (
                typeParameterList: TypeParameterList
                (
                    parameters: method.GetGenericArguments().ToSyntaxList(type => TypeParameter(CreateType(type).ToFullString()))
                )
            );

            if (forceInlining) result = result.WithAttributeLists
            (
                attributeLists: DeclareMethodImplAttributeToForceInlining()
            );

            //
            // Interface metodus nem lehet "async" ezert nem kell ellenorizni h rendelkezik
            // e "AsyncStateMachineAttribute" attributummal.
            //

            return result;
        }

        /// <summary>
        /// int IInterface[T].Prop <br/>
        /// {                <br/>
        ///   get{...}       <br/>
        ///   set{...}       <br/>
        /// }                <br/>
        /// </summary>
        protected internal static PropertyDeclarationSyntax DeclareProperty(PropertyInfo property, CSharpSyntaxNode? getBody = null, CSharpSyntaxNode? setBody = null, bool forceInlining = false)
        {
            Debug.Assert(property.DeclaringType.IsInterface);

            PropertyDeclarationSyntax result = PropertyDeclaration
            (
                type: CreateType(property.PropertyType),
                identifier: Identifier(property.StrippedName())
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(property.DeclaringType))
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (property.CanRead && getBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.GetAccessorDeclaration, getBody, forceInlining));

            if (property.CanWrite && setBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.SetAccessorDeclaration, setBody, forceInlining));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        /// <summary>
        /// int IInterface.this[string index, ...] <br/>
        /// {                                   <br/>
        ///   get{...}                          <br/>
        ///   set{...}                          <br/>
        /// }                                   <br/>
        /// </summary>
        protected internal static IndexerDeclarationSyntax DeclareIndexer(PropertyInfo property, Func<IReadOnlyList<ParameterSyntax>, CSharpSyntaxNode>? getBody = null, Func<IReadOnlyList<ParameterSyntax>, CSharpSyntaxNode>? setBody = null, bool forceInlining = false)
        {
            Debug.Assert(property.DeclaringType.IsInterface);
            Debug.Assert(property.IsIndexer());

            ParameterInfo[] indices = property.GetIndexParameters();

            IndexerDeclarationSyntax result = IndexerDeclaration
            (
                type: CreateType(property.PropertyType)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(property.DeclaringType))
            )
            .WithParameterList
            (
                parameterList: BracketedParameterList
                (
                    parameters: indices.ToSyntaxList
                    (
                        index => Parameter
                        (
                            identifier: Identifier(index.Name)                          
                        )
                        .WithType
                        (
                            type: CreateType(index.ParameterType)
                        )
                    )
                )
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (property.CanRead && getBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.GetAccessorDeclaration, getBody(result.ParameterList.Parameters), forceInlining));

            if (property.CanWrite && setBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.SetAccessorDeclaration, setBody(result.ParameterList.Parameters), forceInlining));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        /// <summary>
        /// [modifier1, modifier2, ....] object Name [= ...];
        /// </summary>
        protected internal static FieldDeclarationSyntax DeclareField<TField>(string name, ExpressionSyntax? initializer = null, params SyntaxKind[] modifiers)
        {
            VariableDeclaratorSyntax declarator = VariableDeclarator
            (
                identifier: Identifier(name)
            );

            if (initializer != null) declarator = declarator.WithInitializer
            (
                initializer: EqualsValueClause(initializer)
            );

            return FieldDeclaration
            (
                VariableDeclaration
                (
                    type: CreateType<TField>()                    
                )
                .WithVariables
                (
                    variables: SingletonSeparatedList(declarator)
                )
            )
            .WithModifiers
            (
                modifiers: TokenList
                (
                    tokens: modifiers.Select(Token)
                )
            );
        }

        /// <summary>
        /// event TDelegate IInterface.EventName <br/>
        /// {                                    <br/>
        ///   add{...}                           <br/>
        ///   remove{...}                        <br/>
        /// }                                    <br/>
        /// </summary>
        protected internal static EventDeclarationSyntax DeclareEvent(EventInfo @event, CSharpSyntaxNode? addBody = null, CSharpSyntaxNode? removeBody = null, bool forceInlining = false)
        {
            Debug.Assert(@event.DeclaringType.IsInterface);

            EventDeclarationSyntax result = EventDeclaration
            (
                type: CreateType(@event.EventHandlerType),
                identifier: Identifier(@event.StrippedName())
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(@event.DeclaringType))
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (@event.AddMethod != null && addBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.AddAccessorDeclaration, addBody, forceInlining));

            if (@event.RemoveMethod != null && removeBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.RemoveAccessorDeclaration, removeBody, forceInlining));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        /// <summary>
        /// target.Event [+|-]= value;
        /// </summary>
        protected internal static AssignmentExpressionSyntax RegisterEvent(EventInfo @event, ExpressionSyntax? target, bool add, Type? castTargetTo = null) => AssignmentExpression
        (
            kind: add ? SyntaxKind.AddAssignmentExpression : SyntaxKind.SubtractAssignmentExpression,
            left: MemberAccess
            (
                target,
                @event,
                castTargetTo
            ),
            right: IdentifierName(Value)
        );

        /// <summary>
        /// target.Property           <br/>
        ///                           <br/>
        /// OR                        <br/>
        ///                           <br/>
        /// target.Propery[index]
        /// </summary>
        protected internal static ExpressionSyntax PropertyAccess(PropertyInfo property, ExpressionSyntax? target, Type? castTargetTo = null) => !property.IsIndexer() 
            ? MemberAccess
            (
                target,
                property,
                castTargetTo
            )
            : (ExpressionSyntax) ElementAccessExpression
            (
                AmendTarget(target, property, castTargetTo),
                BracketedArgumentList
                (
                    arguments: property.GetIndexParameters().ToSyntaxList(param => Argument(IdentifierName(param.Name)))
                )
            );

        /// <summary>
        /// Namespace.Type
        /// </summary>
        protected internal static TypeSyntax CreateType(Type src)
        {
            if (src.IsByRef) src = src.GetElementType();

            if (src == typeof(void)) return PredefinedType(Token(SyntaxKind.VoidKeyword));

            //
            // "Cica<T>.Mica<TT>"-nal a "TT" is beagyazott ami nekunk nem jo
            //

            if (src.IsNested && !src.IsGenericParameter)
            {
                IEnumerable<NameSyntax> partNames;

                IEnumerable<Type> parts = src.GetEnclosingTypes();

                if (!src.IsGenericType) partNames = parts.Append(src).Select(type => GetQualifiedName(type));
                else
                { 
                    //
                    // "Cica<T>.Mica<TT>.Kutya" is generikusnak minosul: Generikus formaban Cica<T>.Mica<TT>.Kutya<T, TT>
                    // mig tipizaltan "Cica<T>.Mica<T>.Kutya<TConcrete1, TConcrete2>".
                    // Ami azert lassuk be igy eleg szopas.
                    //

                    IReadOnlyList<Type> genericArguments = src.GetGenericArguments(); // "<T, TT>" vagy "<TConcrete1, TConcrete2>"

                    partNames = parts.Append(src.GetGenericTypeDefinition()).Select(type =>
                    {
                        int relatedGACount = type.GetOwnGenericArguments().Count();

                        //
                        // Beagyazott tipusnal a GetQualifiedName() a rovid nevet fogja feldolgozni: 
                        // "Cica<T>.Mica<TT>.Kutya<T, TT>" -> "Kutya".
                        //

                        if (relatedGACount > 0)
                        {
                            IEnumerable<Type> relatedGAs = genericArguments.Take(relatedGACount);
                            genericArguments = genericArguments.Skip(relatedGACount).ToArray();

                            return GetQualifiedName(type, name => CreateGenericName(name, relatedGAs.ToArray()));
                        }

                        return GetQualifiedName(type);
                    });
                }

                return Qualify(partNames.ToArray());
            }

            if (src.IsGenericType) return GetQualifiedName(src.GetGenericTypeDefinition(), name => CreateGenericName(name, src.GetGenericArguments()));

            if (src.IsArray) return ArrayType
            (
                elementType: CreateType(src.GetElementType())
            )
            .WithRankSpecifiers
            (
                rankSpecifiers: SingletonList
                (
                    node: ArrayRankSpecifier
                    (
                        sizes: Enumerable.Repeat(0, src.GetArrayRank()).ToSyntaxList(_ => (ExpressionSyntax) OmittedArraySizeExpression())
                    )
                )
            );

            return GetQualifiedName(src);

            NameSyntax CreateGenericName(string name, IReadOnlyCollection<Type> genericArguments) => GenericName(name).WithTypeArgumentList
            (
                typeArgumentList: TypeArgumentList
                (
                    arguments: genericArguments.ToSyntaxList(CreateType)
                )
            );
        }

        /// <summary>
        /// Namespace.Type
        /// </summary>
        protected internal static TypeSyntax CreateType<T>() => CreateType(typeof(T));


        /// <summary>
        /// Namespace.ParentType[T].NestedType[TT]
        /// </summary>
        protected internal static NameSyntax GetQualifiedName(Type type, Func<string, NameSyntax>? typeNameFactory = null)
        {
            Debug.Assert(!type.IsGenericType || type.IsGenericTypeDefinition);

            return Parts2QualifiedName
            (
                parts: type.GetFriendlyName().Split('.').ToArray(),
                factory: typeNameFactory ?? IdentifierName
            );

            NameSyntax Parts2QualifiedName(IReadOnlyCollection<string> parts, Func<string, NameSyntax> factory) => Qualify
            (
                parts
                    //
                    // Nevter, szulo osztaly (beagyazott tipus eseten)
                    //

                    .Take(parts.Count - 1)
                    .Select(part => (NameSyntax) IdentifierName(part))

                    //
                    // Tipus neve
                    //

                    .Append(factory(parts.Last()))
                    .ToArray()
            );
        }

        /// <summary>
        /// Name1.Name2.Name3.....
        /// </summary>
        protected internal static NameSyntax Qualify(params NameSyntax[] parts) => parts.Length <= 1 ? parts.Single() : QualifiedName
        (
            left: Qualify(parts.Take(parts.Length - 1).ToArray()),
            right: (SimpleNameSyntax) parts.Last()
        );

        protected internal static IdentifierNameSyntax ToIdentifierName(LocalDeclarationStatementSyntax variable) => IdentifierName(variable.Declaration.Variables.Single().Identifier);

        /// <summary>
        /// target.Foo(..., ref ..., ...)
        /// </summary>
        protected internal static InvocationExpressionSyntax InvokeMethod(MethodInfo method, ExpressionSyntax? target, Type? castTargetTo = null, params ArgumentSyntax[] arguments)
        {
            IReadOnlyList<ParameterInfo> paramz = method.GetParameters();

            Debug.Assert(arguments.Length == paramz.Count);

            return InvocationExpression
            (
                expression: MethodAccess
                (
                    target,
                    method,
                    castTargetTo
                )
            )
            .WithArgumentList
            (
                argumentList: ArgumentList
                (
                    arguments.ToSyntaxList
                    ( 
                        (arg, i) => (paramz[i].GetParameterKind()) switch
                        {
                            ParameterKind.In => arg.WithRefKindKeyword
                            (
                                refKindKeyword: Token(SyntaxKind.InKeyword)
                            ),       
                            ParameterKind.Out => arg.WithRefKindKeyword
                            (
                                refKindKeyword: Token(SyntaxKind.OutKeyword)
                            ),
                            ParameterKind.InOut => arg.WithRefKindKeyword
                            (
                                refKindKeyword: Token(SyntaxKind.RefKeyword)
                            ),
                            _ => arg
                        }
                    )
                )
            );
        }

        /// <summary>
        /// target.Foo(ref a, b, c)
        /// </summary>
        protected internal static InvocationExpressionSyntax InvokeMethod(MethodInfo method, ExpressionSyntax? target, Type? castTargetTo = null, params string[] arguments)
        {
            IReadOnlyList<ParameterInfo> paramz = method.GetParameters();

            Debug.Assert(arguments.Length == paramz.Count);

            return InvokeMethod
            (
                method,
                target,
                castTargetTo,
                arguments: paramz
                    .Select((param, i) => Argument
                    (
                        expression: IdentifierName(arguments[i])
                    ))
                    .ToArray()
            );
        }

        /// <summary>
        /// TypeName(int a, string b, ...): base(a, b, ...){ }
        /// </summary>
        protected internal ConstructorDeclarationSyntax DeclareCtor(ConstructorInfo ctor)
        {
            IReadOnlyList<ParameterInfo> paramz = ctor.GetParameters();

            return ConstructorDeclaration
            (
                identifier: Identifier(GeneratedClassName)
            )
            .WithModifiers
            (
                modifiers: TokenList
                (
                    Token(SyntaxKind.PublicKeyword)
                )
            )
            .WithParameterList
            (
                parameterList: ParameterList(paramz.ToSyntaxList(param => Parameter
                    (
                        identifier: Identifier(param.Name)
                    )
                    .WithType
                    (
                        type: CreateType(param.ParameterType)
                    )))
            )
            .WithInitializer
            (
                initializer: ConstructorInitializer
                (
                    SyntaxKind.BaseConstructorInitializer,
                    ArgumentList(paramz.ToSyntaxList(param => Argument
                    (
                        expression: IdentifierName(param.Name)
                    )))
                )
            )
            .WithBody(Block());
        }

        protected internal abstract ClassDeclarationSyntax GenerateProxyClass();
        #endregion

        #region Public
        public virtual string GeneratedClassName { get; } = "GeneratedProxy";

        public abstract string AssemblyName { get; }

        public CompilationUnitSyntax GenerateProxyUnit
        (
#if IGNORE_VISIBILITY
            params string[] ignoreAccessChecksTo
#endif
        )
        {
            return CompilationUnit().WithMembers
            (
                members: SingletonList<MemberDeclarationSyntax>
                (
                    GenerateProxyClass()
                )
            )
            .WithAttributeLists
            (
                SingletonList
                (
                    AttributeList
                    (

                        new[] 
                        {
                            CreateAttribute<AssemblyTitleAttribute>(AssemblyName),
                            CreateAttribute<AssemblyDescriptionAttribute>("Generated by ProxyGen.NET")
                        }
#if IGNORE_VISIBILITY
                        .Concat
                        (
                            ignoreAccessChecksTo.Select(asmName => (SyntaxNodeOrToken) CreateAttribute<IgnoresAccessChecksToAttribute>(asmName))

                        )
#endif
                        .ToSyntaxList()
                    )
                    .WithTarget
                    (
                        AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword))
                    )

                )
            );

            AttributeSyntax CreateAttribute<TAttribute>(string param) where TAttribute: Attribute => Attribute
            (
                (NameSyntax) CreateType<TAttribute>()
            )
            .WithArgumentList
            (
                argumentList: AttributeArgumentList
                (
                    arguments: SingletonSeparatedList
                    (
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(param)))
                    )
                )
            );
        }
#endregion
    }
}