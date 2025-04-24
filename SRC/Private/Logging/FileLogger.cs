/********************************************************************************
* FileLogger.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Logger that outputs the logs to a file
    /// </summary>
    internal sealed class FileLogger : ILogger
    {
        private StreamWriter? FLogWriter;
        private readonly string? FSourceDump;

        public FileLogger(string scope, ILogConfiguration config)
        {
            if (config.LogDump is not null)
            {
                Directory.CreateDirectory(config.LogDump);

                FLogWriter = File.CreateText(Path.Combine(config.LogDump, $"{scope}.log"));

                Scope = scope;
                Level = config.LogLevel;
            }

            FSourceDump = config.SourceDump;
            Scope = scope;
        }

        /// <summary>
        /// Disposes this instance. Since this is an internal class we won't implement the disposable pattern.
        /// </summary>
        public void Dispose()
        {
            FLogWriter?.Dispose();
            FLogWriter = null;
        }

        public void WriteSource(CompilationUnitSyntax src)
        {
            if (FSourceDump is null)
                return;

            Directory.CreateDirectory(FSourceDump);

            //
            // Overwrite the target file for every WriteSource invocation
            //

            using StreamWriter srcFile = File.CreateText(Path.Combine(FSourceDump, $"{Scope}.cs"));
            srcFile.Write(src.NormalizeWhitespace(eol: Environment.NewLine).ToFullString());
        }

        public void Log(LogLevel level, object id, string message)
        {
            if (level < Level)
                return;

            FLogWriter?.WriteLine($"{DateTime.UtcNow:o} [{level}] {id} - {message}");
        }

        public LogLevel? Level { get; }

        public string Scope { get; } = null!;
    }
}
