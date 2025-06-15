/********************************************************************************
* IMemberInfo.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Describes an abstract member (for instance property or method).
    /// </summary>
    internal interface IMemberInfo: IHasName
    {
        /// <summary>
        /// The type that declares this member.
        /// </summary>
        ITypeInfo DeclaringType { get; }

        /// <summary>
        /// Returns true if the member is static.
        /// </summary>
        bool IsStatic { get; }

        /// <summary>
        /// Returns true if the member has to be overridden in the derived class (abstract members are also virtual).
        /// </summary>
        bool IsAbstract { get; }

        /// <summary>
        /// Returns true if the member can be overridden in the derived class.
        /// </summary>
        bool IsVirtual { get; }
    }
}
