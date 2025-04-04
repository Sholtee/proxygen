/********************************************************************************
* IInterceptorAccess.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Type independent way to access the underlying interceptor.
    /// </summary>
    public interface IInterceptorAccess
    {
        /// <summary>
        /// The interceptor instance
        /// </summary>
        IInterceptor Interceptor { get; set; }
    }
}
