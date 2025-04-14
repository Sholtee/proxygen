/********************************************************************************
* ClassProxySyntaxFactory.ConstructorFactory.cs                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal partial class ClassProxySyntaxFactory
    {
        #if DEBUG
        internal
        #endif
        protected override ClassDeclarationSyntax ResolveConstructors(ClassDeclarationSyntax cls, object context)
        {
            int found = 0;

            foreach (IConstructorInfo ctor in BaseType.GetConstructors(AccessModifiers.Protected))
                if (IsVisible(ctor))
                {
                    cls = ResolveConstructor(cls, context, ctor);
                    found++;
                }

            if (found is 0)
                throw new InvalidOperationException(string.Format(Resources.NO_ACCESSIBLE_CTOR, BaseType.Name));

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