/********************************************************************************
* ProxyUnitSyntaxFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal abstract class ProxyUnitSyntaxFactory : UnitSyntaxFactoryBase
    {
        protected ProxyUnitSyntaxFactory(OutputType outputType, string containingAssembly, ITypeInfo relatedGenerator, ReferenceCollector? referenceCollector): base(outputType, referenceCollector)
        {
            if (!relatedGenerator.GetBaseTypes().Some(@base => @base.QualifiedName == typeof(Generator).FullName))
                throw new ArgumentException(Resources.NOT_A_GENERATOR, nameof(relatedGenerator));

            RelatedGenerator = relatedGenerator;
            ContainingAssembly = containingAssembly;
        }

        public override ITypeInfo RelatedGenerator { get; }

        public override string ContainingAssembly { get; }

        public override string ContainingNameSpace { get; } = "Proxies";
    }
}