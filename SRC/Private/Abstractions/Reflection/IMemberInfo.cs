/********************************************************************************
* IMemberInfo.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IMemberInfo: IHasName
    {
        ITypeInfo DeclaringType { get; }
        bool IsStatic { get; }
    }
}
