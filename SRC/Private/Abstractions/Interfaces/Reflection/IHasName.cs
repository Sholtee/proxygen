/********************************************************************************
* IHasName.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface IHasName
    {
        /// <summary>
        /// The name of the member not containing special characters ("+", "`", "&lt;", "&gt;"), namespace for nested types or interface part of explicit implementations.
        /// </summary>
        string Name { get; }
    }
}
