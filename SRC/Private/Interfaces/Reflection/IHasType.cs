/********************************************************************************
* IHasType.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IHasType
    {
        ITypeInfo Type { get; }
    }
}
