/********************************************************************************
* ProxyCodeFactory.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Solti.Utils.Proxy.Internals
{
    using Generators;

    internal sealed class ProxyCodeFactory : ICodeFactory
    {
        #pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [ModuleInitializer]
        #pragma warning restore CA2255
        public static void Init() => ICodeFactory.Registered.Entries.Add(new ProxyCodeFactory());

        public bool ShouldUse(ITypeInfo generator) => generator.QualifiedName == typeof(ProxyGenerator<,>).FullName;

        public IEnumerable<SourceCode> GetSourceCodes(ITypeInfo generator, string? assembly, CancellationToken cancellation)
        {
            IGenericTypeInfo genericTypeInfo = (IGenericTypeInfo) generator;

            ITypeInfo
                iface = genericTypeInfo.GenericArguments[0],
                interceptor = genericTypeInfo.GenericArguments[1];

            SourceCode result;

            try
            {
                result = new ProxySyntaxFactory
                (
                    iface,
                    interceptor,
                    assembly,
                    OutputType.Unit,
                    
                    //
                    // Ha nem kell dump-olni a referenciakat akkor felesleges oket osszegyujteni
                    //

                    !string.IsNullOrEmpty(WorkingDirectories.Instance.SourceDump) ? new ReferenceCollector() : null
                ).GetSourceCode(cancellation);
            }
            catch (Exception e) 
            {
                e.Data[nameof(iface)] = iface;
                e.Data[nameof(interceptor)] = interceptor;

                throw;
            }

            //
            // "yield" nem szerepelhet "try" blokkban
            //

            yield return result;
        }
    }
}
