/********************************************************************************
* IAssemblyInfo.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Describes the abstraction of .NET assemblies
    /// </summary>
    internal interface IAssemblyInfo
    {
        /// <summary>
        /// Returns the location of the assembly. Null for dynamic assemblies or compilations where the location is unknown.
        /// </summary>
        string? Location { get; }

        /// <summary>
        /// Returns true if the assembly is dynamic (generated run time in memory).
        /// </summary>
        bool IsDynamic { get; }

        /// <summary>
        /// The name of the assembly
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determines whether the given assembly (identified by the <paramref name="asmName"/>) can access the internal types from this assembly.
        /// </summary>
        bool IsFriend(string asmName);

        /// <summary>
        /// Gets an exported type by name.
        /// </summary>
        ITypeInfo? GetType(string fullName);
    }
}
