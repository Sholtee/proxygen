/********************************************************************************
* IRuntimeContext.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IRuntimeContext
    {
        ITypeInfo? GetTypeByQualifiedName(string name);
    }
}
