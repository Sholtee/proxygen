/********************************************************************************
* ProxyUnitSyntaxFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class ProxyUnitSyntaxFactory(ITypeInfo? targetType, SyntaxFactoryContext context) : ProxyUnitSyntaxFactoryBase(targetType, context)
    {
    }
}