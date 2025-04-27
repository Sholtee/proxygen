/********************************************************************************
* IEventInfo.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IEventInfo: IMemberInfo, IHasType
    {
        IMethodInfo AddMethod { get; }
        IMethodInfo RemoveMethod { get; }
    }
}
