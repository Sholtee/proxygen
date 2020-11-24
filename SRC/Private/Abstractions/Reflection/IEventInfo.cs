/********************************************************************************
* IEventInfo.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IEventInfo: IMemberInfo, IHasType
    {
        IMethodInfo AddMethod { get; }
        IMethodInfo RemoveMethod { get; }
    }
}
