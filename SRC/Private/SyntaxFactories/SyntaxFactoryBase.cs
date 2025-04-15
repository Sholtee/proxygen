/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class SyntaxFactoryBase(SyntaxFactoryContext context)
    {
        public SyntaxFactoryContext Context { get; } = context;
    }
}