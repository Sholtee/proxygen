/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal abstract class SyntaxFactoryBase
    {
        private readonly HashSet<Assembly> FReferences = new HashSet<Assembly>();
        protected readonly List<MemberDeclarationSyntax> FMembers = new List<MemberDeclarationSyntax>();

        public IReadOnlyCollection<MemberDeclarationSyntax> Members => FMembers;

        public IReadOnlyCollection<MetadataReference> GetReferences() => FReferences
            .Select(asm => MetadataReference.CreateFromFile(asm.Location))
            .ToArray();

        public virtual string GeneratedClassName { get; } = "GeneratedProxy";

        protected void AddReference(Type type) 
        {
            if (type.IsGenericTypeDefinition)
                return;

            Assembly asm = type.Assembly;
            
            if (string.IsNullOrEmpty(asm.Location))
                throw new NotSupportedException(Resources.DYNAMIC_ASM);

            foreach (Type genericArg in type.GetGenericArguments() /*ures tomb ha "type" nem generikus*/)
                AddReference(genericArg);

            FReferences.Add(asm);
        }

        protected internal virtual ConstructorDeclarationSyntax DeclareCtor(ConstructorInfo ctor) => ConstructorDeclaration
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
                parameterList: ParameterList(ctor.GetParameters().ToSyntaxList(param => Parameter
                (
                    identifier: Identifier(param.Name)
                )
                .WithType
                (
                    type: CreateType(param.ParameterType)
                )))
            );

        protected internal virtual MethodDeclarationSyntax DeclareMethod(MethodInfo method)
        {
            Type
                declaringType = method.DeclaringType,
                returnType = method.ReturnType;

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

                        if (modifiers.Any()) parameter = parameter.WithModifiers
                        (
                            TokenList(modifiers.Select(Token))
                        );

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

            return result;
        }

        protected internal virtual PropertyDeclarationSyntax DeclareProperty(PropertyInfo property)
        {
            Debug.Assert(property.DeclaringType.IsInterface);

            return PropertyDeclaration
            (
                type: CreateType(property.PropertyType),
                identifier: Identifier(property.StrippedName())
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(property.DeclaringType))
            );
        }

        protected internal virtual IndexerDeclarationSyntax DeclareIndexer(PropertyInfo property)
        {
            Debug.Assert(property.DeclaringType.IsInterface);
            Debug.Assert(property.IsIndexer());

            ParameterInfo[] indices = property.GetIndexParameters();

            return IndexerDeclaration
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
        }

        protected internal virtual EventDeclarationSyntax DeclareEvent(EventInfo @event)
        {
            Debug.Assert(@event.DeclaringType.IsInterface);

            return EventDeclaration
            (
                type: CreateType(@event.EventHandlerType),
                identifier: Identifier(@event.StrippedName())
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(@event.DeclaringType))
            );
        }

        protected internal virtual TypeSyntax CreateType(Type type) 
        {
            if (type.IsByRef) type = type.GetElementType();

            if (type == typeof(void)) return PredefinedType(Token(SyntaxKind.VoidKeyword));

            AddReference(type);

            //
            // "Cica<T>.Mica<TT>"-nal a "TT" is beagyazott ami nekunk nem jo
            //

            if (type.IsNested && !type.IsGenericParameter)
            {
                IEnumerable<Type> parts = type.GetEnclosingTypes();

                if (!type.IsGenericType) 
                    return parts
                        .Append(type)
                        .Select(type => type.GetQualifiedName())
                        .Qualify();

                //
                // "Cica<T>.Mica<TT>.Kutya" eseten "Kutya" is generikusnak minosul: Generikus formaban Cica<T>.Mica<TT>.Kutya<T, TT>
                // mig tipizaltan "Cica<T>.Mica<T>.Kutya<TConcrete1, TConcrete2>". Ami azert lassuk be igy eleg szopas.
                //

                return parts.Append(type.GetGenericTypeDefinition()).Select(type =>
                {
                    IEnumerable<Type> ownGAs = type.GetOwnGenericArguments();

                    //
                    // Beagyazott tipusnal a GetQualifiedName() a rovid nevet fogja feldolgozni: 
                    // "Cica<T>.Mica<TT>.Kutya<T, TT>" -> "Kutya".
                    //

                    return ownGAs.Any()
                        ? type.GetQualifiedName(name => CreateGenericName(name, ownGAs))
                        : type.GetQualifiedName();
                }).Qualify();
            }

            if (type.IsGenericType) 
                return type
                    .GetGenericTypeDefinition()
                    .GetQualifiedName(name => CreateGenericName(name, type.GetGenericArguments()));

            if (type.IsArray) return ArrayType
            (
                elementType: CreateType(type.GetElementType())
            )
            .WithRankSpecifiers
            (
                rankSpecifiers: SingletonList
                (
                    node: ArrayRankSpecifier
                    (
                        sizes: Enumerable.Repeat(0, type.GetArrayRank()).ToSyntaxList(_ => (ExpressionSyntax) OmittedArraySizeExpression())
                    )
                )
            );

            return type.GetQualifiedName();

            NameSyntax CreateGenericName(string name, IEnumerable<Type> genericArguments) => GenericName(name).WithTypeArgumentList
            (
                typeArgumentList: TypeArgumentList
                (
                    arguments: genericArguments.ToSyntaxList(CreateType)
                )
            );
        }

        public virtual void Setup(Type iface, CancellationToken cancellation) 
        {
            foreach (MemberInfo member in iface.ListMembers<MemberInfo>()) 
            {
                cancellation.ThrowIfCancellationRequested();

                switch (member) 
                {
                    case MethodInfo method:
                        FMembers.Add(DeclareMethod(method));
                        break;
                    case PropertyInfo property:
                        FMembers.Add(property.IsIndexer() ? DeclareIndexer(property) : (MemberDeclarationSyntax) DeclareProperty(property));
                        break;
                    case EventInfo evt:
                        FMembers.Add(DeclareEvent(evt));
                        break;
                }
            }
        }
    }
}
