/********************************************************************************
* ITargetAccess.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Type independent way to access the underlying target.
    /// </summary>
    public interface ITargetAccess
    {
        /// <summary>
        /// The target instance.
        /// </summary>
        object? Target { get; set; }
    }
}
