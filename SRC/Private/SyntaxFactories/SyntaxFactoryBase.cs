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

    internal class SyntaxFactoryBase
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

        /// <summary>
        /// Namespace.ParentType[T].NestedType[TT] -> NestedType[TT] <br/>
        /// Namespace.ParentType[T] -> Namespace.ParentType[T]
        /// </summary>
        protected internal virtual NameSyntax GetQualifiedName(Type type)
        {
            //
            // GetFriendlyName() lezart generikusokat nem eszi meg
            //

            IReadOnlyList<string> parts = (type.IsGenericType ? type.GetGenericTypeDefinition() : type)
                .GetFriendlyName()
                .Split('.');

            return parts
                //
                // Nevter, szulo osztaly (beagyazott tipus eseten)
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
                    CreateTypeName(parts[parts.Count - 1], type.GetOwnGenericArguments())
                )
                .Qualify();

            NameSyntax CreateTypeName(string name, IEnumerable<Type> genericArguments) => !genericArguments.Any() ? IdentifierName(name) : GenericName(name).WithTypeArgumentList
            (
                typeArgumentList: TypeArgumentList
                (
                    arguments: genericArguments.ToSyntaxList(CreateType)
                )
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

            if (type.IsNested && !type.IsGenericParameter) return type
                .GetEnclosingTypes()
                .Append(type)
                .Select(GetQualifiedName)
                .Qualify();

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
                        sizes: Enumerable
                            .Repeat(0, type.GetArrayRank())
                            .ToSyntaxList(_ => (ExpressionSyntax) OmittedArraySizeExpression())
                    )
                )
            );

            return GetQualifiedName(type);
        }

        protected virtual CompilationUnitSyntax GenerateProxyUnit(CancellationToken cancellation) => throw new NotImplementedException();

        public (CompilationUnitSyntax Unit, IReadOnlyCollection<MetadataReference> References) GetContext(CancellationToken cancellation = default) => 
        (
            GenerateProxyUnit(cancellation),
            FReferences
                .Select(asm => MetadataReference.CreateFromFile(asm.Location))
                .ToArray()
        );
    }
}
