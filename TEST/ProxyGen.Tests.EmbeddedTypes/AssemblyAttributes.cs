/********************************************************************************
* AssemblyAttributes.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;

using Solti.Utils.Proxy;
using Solti.Utils.Proxy.Attributes;
using Solti.Utils.Proxy.Generators;
using Solti.Utils.Proxy.Tests.External;
using Solti.Utils.Proxy.Tests.EmbeddedTypes;

[
    assembly: 
    EmbedGeneratedType(typeof(ProxyGenerator<IList, ExternalInterceptor<IList>>)),
    EmbedGeneratedType(typeof(ProxyGenerator<IList, InterfaceInterceptor<IList, List<object>>>)),
    EmbedGeneratedType(typeof(ProxyGenerator<IList<string>, ExternalInterceptor<IList<string>>>)),
    EmbedGeneratedType(typeof(ProxyGenerator<IInternalInterface, InterfaceInterceptor<IInternalInterface>>)),
    EmbedGeneratedType(typeof(ProxyGenerator<IGenericInterfaceHavingConstraint, InterfaceInterceptor<IGenericInterfaceHavingConstraint>>)),
    EmbedGeneratedType(typeof(DuckGenerator<IInternalInterface, IInternalInterface>)),
    EmbedGeneratedType(typeof(DuckGenerator<IReadOnlyCollection<string>, List<string>>)),
    EmbedGeneratedType(typeof(ClassProxyGenerator<InternalFoo<string>>)),
    EmbedGeneratedType(typeof(ClassProxyGenerator<List<object>>))
]

