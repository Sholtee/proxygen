/********************************************************************************
* RuntimeConfig.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Configuration read from <see cref="AppContext"/>
    /// </summary>
    internal sealed class RuntimeConfig : ConfigBase, IAssemblyCachingConfiguration
    {
        /// <inheritdoc/>
        public string? AssemblyCacheDir => GetPath(nameof(AssemblyCacheDir));

        /// <inheritdoc/>
        protected override string BasePath { get; } = AppDomain.CurrentDomain.BaseDirectory;

        /// <inheritdoc/>
        protected override string? ReadValue(string name) => AppContext.GetData($"ProxyGen.{name}") as string;
    }
}
