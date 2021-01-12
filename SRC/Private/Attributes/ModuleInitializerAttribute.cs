/********************************************************************************
* ModuleInitializerAttribute.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// https://www.cazzulino.com/module-initializers.html
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
}
