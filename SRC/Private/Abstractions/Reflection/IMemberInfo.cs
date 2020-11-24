/********************************************************************************
* IMemberInfo.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IMemberInfo: IHasName
    {
        ITypeInfo DeclaringType { get; }
        bool IsStatic { get; }
    }
}
