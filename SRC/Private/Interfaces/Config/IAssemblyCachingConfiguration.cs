/********************************************************************************
* IAssemblyCachingConfiguration.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Describes the configuration of assembly caching
    /// </summary>
    internal interface IAssemblyCachingConfiguration 
    {
        /// <summary>
        /// Cache directory where to save the compiled assemblies. When null, the assemblies will be created in memory and won't be persisted.
        /// </summary>
        string? AssemblyCacheDir { get; }
    }
}
