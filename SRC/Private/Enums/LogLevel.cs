/********************************************************************************
* LogLevel.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Available log levels 
    /// </summary>
    internal enum LogLevel
    {
        /// <summary>
        /// Information, represents normal behavior.
        /// </summary>
        Info,

        /// <summary>
        /// Represents a warning, the system can still operate without issue.
        /// </summary>
        /// <remarks>Errors caused by invalid user inputs should alse generate warnings</remarks>
        Warn,

        /// <summary>
        /// Represents an internal error
        /// </summary>
        Error
    }
}
