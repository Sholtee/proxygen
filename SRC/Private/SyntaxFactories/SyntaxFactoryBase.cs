/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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
        private readonly HashSet<Assembly> FReferences = new HashSet<Assembly>(Runtime.Assemblies);

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

        protected abstract CompilationUnitSyntax GenerateProxyUnit(CancellationToken cancellation);

        public (CompilationUnitSyntax Unit, IReadOnlyCollection<MetadataReference> References) GetContext(CancellationToken cancellation = default) => 
        (
            GenerateProxyUnit(cancellation),
            FReferences
                .Select(asm => MetadataReference.CreateFromFile(asm.Location))
                .ToArray()
        );
    }
}
