/********************************************************************************
* LoggerFactory.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// The default implementation of the <see cref="ILoggerFactory"/> interface.
    /// </summary>
    /// <remarks>To create a <see cref="DebugLogger"/> pass null for <paramref name="configuration"/></remarks>
    internal sealed class LoggerFactory(ILogConfiguration? configuration) : ILoggerFactory, IDisposable
    {
        private readonly Stack<IDisposable> FDisposables = [];

        public ILogger CreateLogger(string scope)
        {
            if (configuration?.LogDirectory is not null)
            {
                FileLogger logger = new(scope, configuration);
                FDisposables.Push(logger);
                return logger;
            }

            return new DebugLogger(scope);
        }

        public void Dispose()
        {
            while (FDisposables.Count > 0)
                FDisposables.Pop().Dispose();
        }
    }
}
