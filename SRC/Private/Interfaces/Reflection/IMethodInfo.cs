/********************************************************************************
* IMethodInfo.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Specifies the abstraction of method metadata we want to inspect.  
    /// </summary>
    internal interface IMethodInfo: IMemberInfo
    {
        /// <summary>
        /// Method parameters (not including the <see cref="ReturnValue"/>).
        /// </summary>
        IReadOnlyList<IParameterInfo> Parameters { get; }

        /// <summary>
        /// The interfaces that declares this method.
        /// </summary>
        IReadOnlyList<ITypeInfo> DeclaringInterfaces { get; }

        /// <summary>
        /// The return value of this method
        /// </summary>
        IParameterInfo ReturnValue { get; }

        /// <summary>
        /// Returns true if the method is a backing method (for instance property accessors)
        /// </summary>
        bool IsSpecial { get; }

        /// <summary>
        /// The visibility of this method.
        /// </summary>
        AccessModifiers AccessModifiers { get; }
    }

    /// <summary>
    /// Constructor info
    /// </summary>
    internal interface IConstructorInfo : IMethodInfo { }

    /// <summary>
    /// Generic method info
    /// </summary>
    internal interface IGenericMethodInfo : IMethodInfo, IGeneric<IGenericMethodInfo> { }
}
