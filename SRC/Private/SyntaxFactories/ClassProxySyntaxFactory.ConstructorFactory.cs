/********************************************************************************
* ClassProxySyntaxFactory.ConstructorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ClassProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context)
        {
            foreach (IConstructorInfo ctor in TargetType.GetConstructors(AccessModifiers.Protected))
            {
                Visibility.Check(ctor, ContainingAssembly, allowProtected: true);

                cls = ResolveConstructor(cls, context, ctor);
            }

            return cls;
        }

        /// <summary>
        /// <code>
        /// public MyClass(T param1, TT param2): base(param1, param2) {}
        /// </code>
        /// </summary>
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructor(ClassDeclarationSyntax cls, object context, IConstructorInfo ctor) => cls.AddMembers
        (
            ResolveConstructor(ctor, cls.Identifier)
        );
    }
}