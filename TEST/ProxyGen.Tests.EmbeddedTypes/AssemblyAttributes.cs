/********************************************************************************
* AssemblyAttributes.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

using Solti.Utils.Proxy.Attributes;
using Solti.Utils.Proxy.Generators;
using Solti.Utils.Proxy.Tests.EmbeddedTypes;
[
    assembly: 
    EmbedGeneratedType(typeof(InterfaceProxyGenerator<IList>)),
    EmbedGeneratedType(typeof(InterfaceProxyGenerator<IList<string>>)),
    EmbedGeneratedType(typeof(InterfaceProxyGenerator<IInternalInterface>)),
    EmbedGeneratedType(typeof(InterfaceProxyGenerator<IGenericInterfaceHavingConstraint>)),
    EmbedGeneratedType(typeof(DuckGenerator<IInternalInterface, IInternalInterface>)),
    EmbedGeneratedType(typeof(DuckGenerator<IReadOnlyCollection<string>, List<string>>)),
    EmbedGeneratedType(typeof(ClassProxyGenerator<InternalFoo<string>>)),
    EmbedGeneratedType(typeof(ClassProxyGenerator<List<object>>)),
    EmbedGeneratedType(typeof(DelegateProxyGenerator<Func<int, object>>)),
    EmbedGeneratedType(typeof(DelegateProxyGenerator<Action<object>>)),
    EmbedGeneratedType(typeof(DelegateProxyGenerator<InternalDelegate<object>>))
]

