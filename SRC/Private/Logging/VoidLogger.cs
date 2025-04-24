/********************************************************************************
* VoidLogger.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Logger that logs nothing.
    /// </summary>
    internal sealed  class VoidLogger: ILogger
    {
        public void WriteSource(CompilationUnitSyntax src) { }

        public void Log(LogLevel level, object id, string message) { }

        public LogLevel? Level { get; }

        public string Scope { get; } = null!;

        public void Dispose() { }
    }
}
