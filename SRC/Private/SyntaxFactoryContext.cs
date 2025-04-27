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
        /// The logger factory associated with this context. Concrete loggers can be instantiated by providing a log scope.
        /// </summary>
        public required ILoggerFactory LoggerFactory { get; init; }

        /// <summary>
        /// The reference collector instance or null if the source is being embedded.
        /// </summary>
        public IReferenceCollector? ReferenceCollector { get; init; }

        /// <summary>
        /// The name of assembly being compiled. Intended for test purposes
        /// </summary>
        public string? AssemblyNameOverride { get; init; }

        /// <summary>
        /// Language version to be used.
        /// </summary>
        public LanguageVersion LanguageVersion { get; init; } = LanguageVersion.CSharp9;

        /// <summary>
        /// The default configuration. Set up for module builds.
        /// </summary>
        public static SyntaxFactoryContext Default { get; } = new()
        {
            OutputType = OutputType.Module,

            //
            // We don't need to dispose the LoggerFactory as it always returns DebugLogger instances
            //

            LoggerFactory = new LoggerFactory(null)
        };
    }
}