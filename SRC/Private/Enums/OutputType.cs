/********************************************************************************
* OutputType.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Supported syntax factory output types
    /// </summary>
    internal enum OutputType 
    {
        /// <summary>
        /// The generated source is intended to be built separately.
        /// </summary>
        /// <remarks>This is the default value.</remarks>
        Module,

        /// <summary>
        /// The generated source is intended to be embedded by a <see href="https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/">source generator</see> 
        /// </summary>
        Unit
    }
}
