/********************************************************************************
* ProxySyntaxFactory.InterceptorFactoryBase.cs                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    internal partial class ProxySyntaxFactory<TInterface, TInterceptor> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        internal abstract class InterceptorFactoryBase: ProxySyntaxFactory<TInterface, TInterceptor>, IInterceptorFactory
        {
            //
            // Az interceptor altal mar implementalt interface-ek ne szerepeljenek a proxy deklaracioban.
            //

            protected static bool AlreadyImplemented(IMemberInfo member) => MetadataTypeInfo
                .CreateFrom(typeof(TInterceptor))
                .Interfaces
                .Contains(member.DeclaringType);

            public abstract MemberDeclarationSyntax Build(IMemberInfo member);

            public virtual bool IsCompatible(IMemberInfo member) => member.DeclaringType.IsInterface && !AlreadyImplemented(member);
        }
    }
}