/********************************************************************************
* IInterceptorAccess.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Type independent way to access the underlying interceptor.
    /// </summary>
    /// <remarks>This interface is used by the generated proxies only therefore it is considered internal. User code should not depend on it.</remarks>
    public interface IInterceptorAccess
    {
        /// <summary>
        /// The interceptor instance
        /// </summary>
        IInterceptor Interceptor { get; set; }
    }
}