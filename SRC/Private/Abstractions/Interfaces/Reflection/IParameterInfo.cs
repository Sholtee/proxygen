/********************************************************************************
* IParameterInfo.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IParameterInfo: IHasName, IHasType
    {
        ParameterKind Kind { get; }
    }
}
