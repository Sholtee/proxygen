/********************************************************************************
* ClassSyntaxFactoryBase.Constructor.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassSyntaxFactoryBase
    {
        /// <summary>
        /// <code>
        /// TypeName(T a, TT b, ...): base(a, b, ...) { }
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected virtual ConstructorDeclarationSyntax ResolveConstructor(IConstructorInfo ctor, SyntaxToken name) => ConstructorDeclaration(name)
            .WithModifiers
            (
                modifiers: TokenList
                (
                    //
                    // Constructor argument may use nested protected/internal types
                    //

                    Token
                    (
                        ctor.Parameters.SelectMany(static param => param.Type.AccessModifiers.SetFlags()).DefaultIfEmpty().Min() switch
                        {
                            AccessModifiers.Protected => SyntaxKind.ProtectedKeyword,
                            AccessModifiers.Internal => SyntaxKind.InternalKeyword,
                            _ => SyntaxKind.PublicKeyword
                        }
                    )
                )
            )
            .WithParameterList
            (
                parameterList: ResolveParameterList(ctor)
            )
            .WithInitializer
            (
                initializer: ConstructorInitializer
                (
                    SyntaxKind.BaseConstructorInitializer,
                    ResolveArgumentList
                    (
                        ctor,
                        ctor.Parameters.Select
                        (
                            static param => Argument
                            (
                                expression: IdentifierName(param.Name)
                            )
                        )
                    )
                )
            )
            .WithBody(Block());

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context) => cls;

        #if DEBUG
        internal
        #endif
        protected virtual ClassDeclarationSyntax ResolveConstructor(ClassDeclarationSyntax cls, object context, IConstructorInfo ctor) => cls;

        protected static string EnsureUnused(ConstructorDeclarationSyntax ctor, string param) => EnsureUnused
        (
            ctor.ParameterList.Parameters.Select(static p => p.Identifier.Text),
            param
        );
    }
}
