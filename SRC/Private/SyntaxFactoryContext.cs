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
    internal sealed class SyntaxFactoryContext
    {
        /// <summary>
        /// The output type. <see cref="OutputType.Unit"/> for source embedding and <see cref="OutputType.Module"/> for module generation.
        /// </summary>
        public required OutputType OutputType { get; init; }

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

        public static SyntaxFactoryContext Default = new()
        {
            OutputType = OutputType.Module
        };
    }
}