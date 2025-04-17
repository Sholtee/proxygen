/********************************************************************************
* SyntaxFactoryBase.Type.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class SyntaxFactoryBase
    {
        /// <summary>
        /// <code>
        /// Namespace.ParentType[T].NestedType[TT] -> NestedType[TT] 
        /// Namespace.ParentType[T] -> global::Namespace.ParentType[T]
        /// </code>
        /// </summary>
        private NameSyntax GetQualifiedName(ITypeInfo type)
        {
            Context.ReferenceCollector?.AddType(type);

            string[] parts = type.Name.Split(Type.Delimiter);

            NameSyntax[] names = new NameSyntax[parts.Length];

            if (type.Flags.HasFlag(TypeInfoFlags.IsNested))
            {
                names[0] = CreateTypeName(parts.Single());
            }
            else
            {
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    names[i] = IdentifierName(parts[i]);
                }

                names[^1] = CreateTypeName(parts[^1]);

                //
                // This handles types having no namespace properly
                //

                if (!type.Flags.HasFlag(TypeInfoFlags.IsVoid) && !type.Flags.HasFlag(TypeInfoFlags.IsGenericParameter)) names[0] = AliasQualifiedName
                (
                    IdentifierName
                    (
                        Token(SyntaxKind.GlobalKeyword)
                    ),
                    (SimpleNameSyntax) names[0]
                );
            }

            return names.Qualify();

            NameSyntax CreateTypeName(string name) => type is not IGenericTypeInfo genericType ? IdentifierName(name) : GenericName(name).WithTypeArgumentList
            (
                typeArgumentList: TypeArgumentList
                (
                    arguments: genericType.GenericArguments.ToSyntaxList(ResolveType)
                )
            );
        }

        #if DEBUG
        internal
        #endif
        protected TypeSyntax ResolveType(ITypeInfo type) 
        {
            //
            // We don't want to invoke AddType() on int[] or int*
            //
            // TODO: FIXME: We should check if "type.RefType > RefType.None"
            //

            if (type.ElementType is not null && type is not IArrayTypeInfo)
            {
                TypeSyntax result = ResolveType(type.ElementType);

                if (type.RefType is RefType.Pointer) 
                    result = AllowPointers ? PointerType(result) : throw new InvalidOperationException(Resources.UNSAFE_CONTEXT);

                return result;
            }

            if (type.Flags.HasFlag(TypeInfoFlags.IsVoid))
            {
                return PredefinedType
                (
                    Token(SyntaxKind.VoidKeyword)
                );
            }

            if (type.Flags.HasFlag(TypeInfoFlags.IsNested))
            {
                return type.GetParentTypes().Append(type).Select(GetQualifiedName).Qualify();
            }

            if (type is IArrayTypeInfo array)
            {
                return ArrayType
                (
                    elementType: ResolveType(array.ElementType!)
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
                                .ToSyntaxList(static arSize => (ExpressionSyntax) arSize)
                        )
                    )
                );
            }

            return GetQualifiedName(type);
        }

        public bool AllowPointers { get; set; }

        #if DEBUG
        internal
        #endif
        protected TypeSyntax ResolveType<T>() => ResolveType
        (
            MetadataTypeInfo.CreateFrom(typeof(T))
        );
    }
}