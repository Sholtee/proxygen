/********************************************************************************
* ITypeGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Abstractions
{
    using Internals;

    /// <summary>
    /// Describes a type generator.
    /// </summary>
    public interface ITypeGenerator
    {
        /// <summary>
        /// The factory that provides the class definition.
        /// </summary>
        IUnitSyntaxFactory SyntaxFactory { get; }
    }
}
