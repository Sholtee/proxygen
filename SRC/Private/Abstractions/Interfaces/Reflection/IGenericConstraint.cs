/********************************************************************************
* IGenericConstraint.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal interface IGenericConstraint
    {
        /// <summary>
        /// The type must have a default constructor: <code>where T: new()</code>
        /// </summary>
        bool DefaultConstructor { get; }
        /// <summary>
        /// The type must be a reference type: <code>where T: class</code>
        /// </summary>
        bool Reference { get; }
        /// <summary>
        /// The type must be a value type: <code>where T: struct</code>
        /// </summary>
        bool Struct { get; }
    }
}
