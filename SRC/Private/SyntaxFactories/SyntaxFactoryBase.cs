/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy.Internals
{
    internal abstract partial class SyntaxFactoryBase
    {
        protected ReferenceCollector? ReferenceCollector { get; }

        public SyntaxFactoryBase(ReferenceCollector? referenceCollector) => ReferenceCollector = referenceCollector;
    }
}