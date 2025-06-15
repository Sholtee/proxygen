/********************************************************************************
* IEventInfo.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Specifies the abstraction of event metadata we want to inspect.  
    /// </summary>
    internal interface IEventInfo: IMemberInfo, IHasType
    {
        /// <summary>
        /// The method to be used for registering an event listener. It is never null.
        /// </summary>
        IMethodInfo AddMethod { get; }

        /// <summary>
        /// The method to be used for removing an event listener. It is never null.
        /// </summary>
        IMethodInfo RemoveMethod { get; }
    }
}
