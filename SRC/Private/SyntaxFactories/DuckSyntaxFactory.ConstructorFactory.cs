/********************************************************************************
* DuckSyntaxFactory.ConstructorFactory.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class DuckSyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context)
        {
            foreach (IConstructorInfo ctor in BaseType.GetConstructors(AccessModifiers.Public))
            {
                cls = ResolveConstructor(cls, context, ctor);
            }
            return cls;
        }

        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructor(ClassDeclarationSyntax cls, object context, IConstructorInfo ctor) => cls.AddMembers
        (
            ResolveConstructor(ctor, cls.Identifier)
        );
    }
}