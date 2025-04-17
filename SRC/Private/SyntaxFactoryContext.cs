/********************************************************************************
* SyntaxFactoryContext.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Common context used by syntax factories
    /// </summary>
    internal sealed record SyntaxFactoryContext
    {
        /// <summary>
        /// The output type. <see cref="OutputType.Unit"/> for source embedding and <see cref="OutputType.Module"/> for module generation.
        /// </summary>
        public required OutputType OutputType { get; init; }

        /// <summary>
        /// The configuration associated with the compilation.
        /// </summary>
        public required Config Config { get; init; }

        /// <summary>
        /// The reference collector instance or null if the source is being embedded.
        /// </summary>
        public ReferenceCollector? ReferenceCollector { get; init; }

        /// <summary>
        /// The name of assembly being compiled. Intended for test purposes
        /// </summary>
        public string? AssemblyNameOverride { get; init; }

        /// <summary>
        /// Language version to be used.
        /// </summary>
        public LanguageVersion LanguageVersion { get; init; } = LanguageVersion.Latest;

        /// <summary>
        /// The default configuration. Set up for module builds.
        /// </summary>
        public static SyntaxFactoryContext Default { get; } = new()
        {
            OutputType = OutputType.Module,
            Config = new Config
            (
                new RuntimeConfigReader()
            )
        };
    }
}