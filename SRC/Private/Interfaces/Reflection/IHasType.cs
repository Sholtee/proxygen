/********************************************************************************
* IHasType.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Describes a member that has type (for instance property, event, or parameter)
    /// </summary>
    internal interface IHasType
    {
        /// <summary>
        /// The member type.
        /// </summary>
        ITypeInfo Type { get; }
    }
}
