/********************************************************************************
* ParameterKind.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    /// <summary>
    /// Describes the possible parameter modifiers.
    /// </summary>
    internal enum ParameterKind
    {
        /// <summary>
        /// No modifier is applied.
        /// </summary>
        None,

        /// <summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#params-modifier">params</see> keyword 
        /// </summary>
        Params,

        /// <summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#in-parameter-modifier">in</see> keyword
        /// </summary>
        In,

        /// <summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#out-parameter-modifier">out</see> keyword
        /// </summary>
        Out,

        /// <summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#in-parameter-modifier">ref</see> keyword
        /// </summary>
        Ref,

        /// <summary>
        /// <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters#ref-readonly-modifier">ref readonly</see> keyword 
        /// </summary>
        RefReadonly
    }
}
