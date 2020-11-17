/********************************************************************************
* ProxySyntaxFactory.InterceptorFactoryBase.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory<TInterface, TInterceptor> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        internal abstract class InterceptorFactoryBase : IInterceptorFactory
        {
            protected ProxySyntaxFactory<TInterface, TInterceptor> Owner { get; }

            protected InterceptorFactoryBase(ProxySyntaxFactory<TInterface, TInterceptor> owner) => Owner = owner;

            public abstract MemberDeclarationSyntax Build(MemberInfo member);

            //
            // Az interceptor altal mar implementalt interface-ek ne szerepeljenek a proxy deklaracioban.
            //

            protected static bool AlreadyImplemented(MemberInfo member) => typeof(TInterceptor).GetInterfaces().Contains(member.DeclaringType);

            public abstract bool IsCompatible(MemberInfo member);
        }
    }
}