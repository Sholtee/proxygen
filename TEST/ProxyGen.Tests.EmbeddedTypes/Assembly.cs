using System.Collections;
using System.Collections.Generic;

using Solti.Utils.Proxy.Attributes;
using Solti.Utils.Proxy.Generators;
using Solti.Utils.Proxy.Tests.External;

[assembly: 
    EmbedGeneratedType(typeof(ProxyGenerator<IList, ExternalInterceptor<IList>>)),
    EmbedGeneratedType(typeof(ProxyGenerator<IList<string>, ExternalInterceptor<IList<string>>>)),
    EmbedGeneratedType(typeof(DuckGenerator<IList, IList>))]