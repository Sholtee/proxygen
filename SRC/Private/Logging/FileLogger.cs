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
        private readonly StreamWriter? FLogWriter;
        private readonly string? FLogDirectory;

        public FileLogger(string scope, ILogConfiguration config)
        {
            FLogDirectory = config.LogDirectory;
            if (FLogDirectory is null)
                return;

            Directory.CreateDirectory(FLogDirectory);

            FLogWriter = File.CreateText(Path.Combine(FLogDirectory, $"{scope}.log"));
            FLogWriter.AutoFlush = true;

            Scope = scope;
            Level = config.LogLevel;
        }

        /// <summary>
        /// Disposes this instance. Since this is an internal class we won't implement the disposable pattern.
        /// </summary>
        public void Dispose() => FLogWriter?.Dispose();

        public void WriteSource(CompilationUnitSyntax src)
        {
            if (FLogDirectory is null)
                return;

            //
            // Overwrite the target file for every WriteSource invocation
            //

            using StreamWriter srcFile = File.CreateText(Path.Combine(FLogDirectory, $"{Scope}.cs"));
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
