/********************************************************************************
* ILoggerFactory.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface ILoggerFactory
    {
        /// <summary>
        /// Creates a new logger using the given log <paramref name="scope"/>.
        /// </summary>
        ILogger CreateLogger(string scope);
    }
}
