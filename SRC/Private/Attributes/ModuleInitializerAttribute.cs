/********************************************************************************
* ModuleInitializerAttribute.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace System.Runtime.CompilerServices
{
#if !NET5_0_OR_GREATER
    /// <summary>
    /// https://www.cazzulino.com/module-initializers.html
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
#endif
}