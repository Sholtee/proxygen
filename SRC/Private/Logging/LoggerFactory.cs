/********************************************************************************
* LoggerFactory.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// The default implementation of the <see cref="ILoggerFactory"/> interface.
    /// </summary>
    /// <remarks>To create a <see cref="VoidLogger"/> pass null for <paramref name="configuration"/></remarks>
    internal sealed class LoggerFactory(ILogConfiguration? configuration) : ILoggerFactory
    {
        public ILogger CreateLogger(string scope) => configuration?.LogDump is not null || configuration?.SourceDump is not null
            ? new FileLogger(scope, configuration)
            : new VoidLogger();
    }
}
