/********************************************************************************
* IParameterInfo.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Abstraction of method parameters.
    /// </summary>
    internal interface IParameterInfo: IHasName, IHasType
    {
        /// <summary>
        /// Parameter kind (in, out, params, etc)
        /// </summary>
        ParameterKind Kind { get; }
    }
}
