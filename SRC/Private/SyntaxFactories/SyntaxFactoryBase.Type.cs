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
    internal partial class SyntaxFactoryBase
    {
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
            //
            // Ez ne  a switch-ben legyen mert az AddType()-ot nem akarjuk hivni int[]-re v int*-ra
            //
            // TODO: FIXME: itt "type.RefType > RefType.None"-ra kene vizsgalni
            //

            if (type.ElementType is not null && type is not IArrayTypeInfo)
            {
                TypeSyntax result = CreateType(type.ElementType!);

                if (type.RefType == RefType.Pointer) 
                    result = PointerType(result);

                return result;
            }

            AddType(type);

            return type switch
            {
                _ when type.IsVoid => PredefinedType
                (
                    Token(SyntaxKind.VoidKeyword)
                ),

                _ when type.IsNested => type
                    .GetParentTypes()
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

        protected internal TypeOfExpressionSyntax TypeOf(ITypeInfo type) => TypeOfExpression
        (
            CreateType(type)
        );
    }
}