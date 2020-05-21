/********************************************************************************
* AssemblyAttributes.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Resources;
using System.Runtime.CompilerServices;

[
    assembly:
        NeutralResourcesLanguage("en"),
        InternalsVisibleTo("ProxyGen.Perf")
#if DEBUG
        , 
        InternalsVisibleTo("Solti.Utils.Proxy.Tests")
#endif
]
