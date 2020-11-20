/********************************************************************************
* SyntaxFactoryBase.Type.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class SyntaxFactoryBase
    {
        private readonly HashSet<Assembly> FReferences = new HashSet<Assembly>(Runtime.Assemblies);

        private readonly HashSet<Type> FTypes = new HashSet<Type>();

        protected internal void AddType(Type type) 
        {
            if (type.IsGenericTypeDefinition)
                return;

            if (!FTypes.Add(type)) return; // korkoros referencia fix

            Assembly asm = type.Assembly;

            if (asm.IsDynamic)
                throw new NotSupportedException(Resources.DYNAMIC_ASM);

            FReferences.Add(asm);

            //
            // Generikus parameterek szerepelhetnek masik szerelvenyben.
            //

            foreach (Type genericArg in type.GetGenericArguments())
                AddType(genericArg);
  
            //
            // Az os (osztaly) szerepelhet masik szerelvenyben. "BaseType" csak az os osztalyokat adja vissza
            // megvalositott interfaceket nem.
            //

            foreach (Type @base in type.GetBaseTypes())
                AddType(@base);

            //
            // "os" interface-ek szarmazhatnak masik szerelvenybol. A GetInterfaces() az osszes "os"-t
            // visszaadja.
            //

            foreach (Type iface in type.GetInterfaces())
                AddType(iface);
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

            AddType(type);

            return type switch
            {
                _ when type == typeof(void) => PredefinedType
                (
                    Token(SyntaxKind.VoidKeyword)
                ),

                //
                // "Cica<T>.Mica<TT>"-nal a "TT" is beagyazott ami nekunk nem jo
                //

                _ when type.IsNested && !type.IsGenericParameter => type
                    .GetEnclosingTypes()
                    .Append(type)
                    .Select(GetQualifiedName)
                    .Qualify(),

                _ when type.IsArray => ArrayType
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
                ),

                _ => GetQualifiedName(type)
            };
        }

        /// <summary>
        /// Namespace.Type
        /// </summary>
        protected internal TypeSyntax CreateType<T>() => CreateType(typeof(T));
    }
}