/********************************************************************************
* ISourceGeneratorConfiguration.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Describes the configuration of our source generator (<see cref="ProxyEmbedder"/>).
    /// </summary>
    internal interface ISourceGeneratorConfiguration
    {
        /// <summary>
        /// Returns true if we want to debug the generator.
        /// </summary>
        bool DebugGenerator { get; }
    }
}
