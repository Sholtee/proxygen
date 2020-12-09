/********************************************************************************
* IHasTarget.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IHasTarget<T>
    {
        T Target { get; }
    }
}
