/********************************************************************************
* FileLogger.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.IO;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Logger that outputs the logs to a file
    /// </summary>
    internal sealed class FileLogger : LoggerBase
    {
        private readonly StreamWriter FLogWriter;
        private readonly string FLogDirectory;

        protected override void LogCore(string message) =>
            FLogWriter.WriteLine(message);

        protected override void WriteSourceCore(string src) =>
            //
            // Overwrite the target file for every invocation
            //

            File.WriteAllText(Path.Combine(FLogDirectory, $"{Scope}.cs"), src);

        public FileLogger(string scope, ILogConfiguration config): base(scope, config.LogLevel)
        {
            FLogDirectory = config.LogDirectory!;

            Directory.CreateDirectory(FLogDirectory);

            FLogWriter = File.CreateText(Path.Combine(FLogDirectory, $"{Scope}.log"));
            FLogWriter.AutoFlush = true;
        }

        /// <summary>
        /// Disposes this instance. Since this is an internal class we won't implement the disposable pattern.
        /// </summary>
        public override void Dispose() => FLogWriter?.Dispose();
    }
}
