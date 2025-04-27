/********************************************************************************
* IHasName.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IHasName
    {
        /// <summary>
        /// The name of the member not containing special characters ("+", "`", "&lt;", "&gt;"), namespace for nested types or interface part of explicit implementations.
        /// </summary>
        string Name { get; }
    }
}
