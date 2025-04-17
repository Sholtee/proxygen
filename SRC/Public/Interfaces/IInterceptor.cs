/********************************************************************************
* IInterceptor.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Describes an abstract interceptor
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// Method to be called on proxy invocation.
        /// </summary>
        object? Invoke(IInvocationContext context);
    }
}
