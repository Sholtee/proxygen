/********************************************************************************
* SyntaxFactoryBase.Type.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        protected internal NameSyntax GetQualifiedName(ITypeInfo type)
        {
            string[] parts = type.Name.Split(Type.Delimiter);

            NameSyntax[] names = new NameSyntax[parts.Length];

            if (type.IsNested)
            {
                Debug.Assert(parts.Length == 1);

                names[0] = CreateTypeName(parts[0]);
            }
            else
            {
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    names[i] = IdentifierName(parts[i]);
                }

                names[names.Length - 1] = CreateTypeName(parts[parts.Length - 1]);

                //
                // Ez jol kezeli azt az esetet is ha a tipus nincs nevter alatt
                //

                if (!type.IsVoid && !type.IsGenericParameter) names[0] = AliasQualifiedName
                (
                    IdentifierName(Token(SyntaxKind.GlobalKeyword)),
                    (SimpleNameSyntax) names[0]
                );
            }

            return names.Qualify();

            NameSyntax CreateTypeName(string name) => type is not IGenericTypeInfo genericType ? IdentifierName(name) : GenericName(name).WithTypeArgumentList
            (
                typeArgumentList: TypeArgumentList
                (
                    arguments: genericType.GenericArguments.ToSyntaxList(CreateType)
                )
            );
        }

        protected internal TypeSyntax CreateType(ITypeInfo type) 
        {
            //
            // Ez ne  a switch-ben legyen mert az AddType()-ot nem akarjuk hivni int[]-re v int*-ra
            //
            // TODO: FIXME: itt "type.RefType > RefType.None"-ra kene vizsgalni
            //

            if (type.ElementType is not null && type is not IArrayTypeInfo)
            {
                TypeSyntax result = CreateType(type.ElementType);

                if (type.RefType is RefType.Pointer) 
                    result = PointerType(result);

                return result;
            }

            AddType(type);

            if (type.IsVoid)
            {
                return PredefinedType
                (
                    Token(SyntaxKind.VoidKeyword)
                );
            }

            if (type.IsNested)
            {
                return GetParts().Qualify();

                IEnumerable<NameSyntax> GetParts()
                {
                    foreach (ITypeInfo parent in type.GetParentTypes())
                    {
                        yield return GetQualifiedName(parent);
                    }

                    yield return GetQualifiedName(type);
                }
            }

            if (type is IArrayTypeInfo array)
            {
                return ArrayType
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
                );
            }

            return GetQualifiedName(type);
        }

        protected internal TypeSyntax CreateType<T>() => CreateType(MetadataTypeInfo.CreateFrom(typeof(T)));

        protected internal TypeOfExpressionSyntax TypeOf(ITypeInfo type) => TypeOfExpression
        (
            CreateType(type)
        );
    }
}