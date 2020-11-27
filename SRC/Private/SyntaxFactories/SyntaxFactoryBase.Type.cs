/********************************************************************************
* SyntaxFactoryBase.Type.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class SyntaxFactoryBase
    {
        private readonly HashSet<IAssemblyInfo> FReferences = new HashSet<IAssemblyInfo>
        (
            Runtime.Assemblies.Select(MetadataAssemblyInfo.CreateFrom),
            IAssemblyInfoComparer.Instance
        );

        private readonly HashSet<ITypeInfo> FTypes = new HashSet<ITypeInfo>(ITypeInfoComparer.Instance);

        protected internal void AddTypesFrom(ISyntaxFactory syntax) 
        {
            foreach (ITypeInfo type in syntax.Types)
                FTypes.Add(type);

            foreach (IAssemblyInfo asm in syntax.References)
                FReferences.Add(asm);
        }

        protected internal void AddType(ITypeInfo type) 
        {
            IGenericTypeInfo? genericType = type as IGenericTypeInfo;

            if (genericType?.IsGenericDefinition == true)
                return;

            if (!FTypes.Add(type)) // korkoros referencia fix
                return;

            IAssemblyInfo asm = type.DeclaringAssembly;

            if (asm.IsDynamic)
                throw new NotSupportedException(Resources.DYNAMIC_ASM);

            FReferences.Add(asm);

            //
            // Generikus parameterek szerepelhetnek masik szerelvenyben.
            //

            if (genericType != null)
                foreach (ITypeInfo genericArg in genericType.GenericArguments)
                    AddType(genericArg);
  
            //
            // Az os (osztaly) szerepelhet masik szerelvenyben.
            //

            foreach (ITypeInfo @base in type.Bases)
                AddType(@base);

            //
            // "os" interface-ek szarmazhatnak masik szerelvenybol.
            //

            foreach (ITypeInfo iface in type.Interfaces)
                AddType(iface);
        }

        /// <summary>
        /// Namespace.ParentType[T].NestedType[TT] -> NestedType[TT] <br/>
        /// Namespace.ParentType[T] -> global::Namespace.ParentType[T]
        /// </summary>
        protected internal virtual NameSyntax GetQualifiedName(ITypeInfo type)
        {
            IReadOnlyList<string> parts = type.Name.Split(Type.Delimiter);

            if (type.IsNested) 
            {
                Debug.Assert(parts.Count == 1);

                return parts
                    .Select(CreateTypeName)
                    .Qualify();
            }

            NameSyntax[] names = parts
                //
                // Nevter
                //
#if NETSTANDARD2_0
                .Take(parts.Count - 1)
#else
                .SkipLast(1)
#endif
                .Select(IdentifierName)

                //
                // Tipus neve
                //

                .Append
                (
                    CreateTypeName
                    (
                        parts[parts.Count - 1]
                    )
                )
                .ToArray();

            //
            // Ez jol kezeli azt az esetet is ha a tipus nincs nevter alatt
            //

            if (!type.IsVoid && !type.IsGenericParameter) names[0] = AliasQualifiedName
            (
                IdentifierName(Token(SyntaxKind.GlobalKeyword)), 
                (SimpleNameSyntax) names[0]
            );

            return names.Qualify();

            NameSyntax CreateTypeName(string name) => type is not IGenericTypeInfo genericType ? IdentifierName(name) : GenericName(name).WithTypeArgumentList
            (
                typeArgumentList: TypeArgumentList
                (
                    arguments: genericType.GenericArguments.ToSyntaxList(CreateType)
                )
            );
        }

        protected internal virtual TypeSyntax CreateType(ITypeInfo type) 
        {
            if (type.IsByRef) type = type.ElementType!;

            AddType(type);

            return type switch
            {
                _ when type.IsVoid => PredefinedType
                (
                    Token(SyntaxKind.VoidKeyword)
                ),

                _ when type.IsNested => type
                    .EnclosingTypes
                    .Append(type)
                    .Select(GetQualifiedName)
                    .Qualify(),

                _ when type is IArrayTypeInfo array => ArrayType
                (
                    elementType: CreateType(array.ElementType!)
                )
                .WithRankSpecifiers
                (
                    rankSpecifiers: SingletonList
                    (
                        node: ArrayRankSpecifier
                        (
                            sizes: array
                                .Rank
                                .Times(OmittedArraySizeExpression)
                                .ToSyntaxList(arSize => (ExpressionSyntax) arSize)
                        )
                    )
                ),

                _ => GetQualifiedName(type)
            };
        }

        protected internal TypeSyntax CreateType<T>() => CreateType(MetadataTypeInfo.CreateFrom(typeof(T)));
    }
}