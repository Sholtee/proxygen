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
        /// The name of the member not containing special characters ("+", "`", "&lt;", "&gt;") or namespace for nested types.
        /// </summary>
        string Name { get; }
    }
}
