/********************************************************************************
* IProxyContext.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IProxyContext
    {
        ITypeInfo InterfaceType { get; }

        ITypeInfo InterceptorType { get; }

        ITypeInfo BaseInterceptorType { get; }

        string ClassName { get; }
    }
}
