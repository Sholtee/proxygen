/********************************************************************************
* ReferenceCollector.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal class ReferenceCollector
    {
        public ReferenceCollector(bool includeRuntimeReferences = true) => FReferences = new HashSet<IAssemblyInfo>
        (
            includeRuntimeReferences
                ? Runtime.Assemblies.Select(MetadataAssemblyInfo.CreateFrom)
                : Array.Empty<IAssemblyInfo>(),
            IAssemblyInfoComparer.Instance
        );

        private readonly HashSet<IAssemblyInfo> FReferences;

        public IReadOnlyCollection<IAssemblyInfo> References => FReferences;

        private readonly HashSet<ITypeInfo> FTypes = new HashSet<ITypeInfo>(ITypeInfoComparer.Instance);

        public IReadOnlyCollection<ITypeInfo> Types => FTypes;

        protected internal void AddTypesFrom(ISyntaxFactory syntax) 
        {
            foreach (ITypeInfo type in syntax.Types)
                FTypes.Add(type);

            foreach (IAssemblyInfo asm in syntax.References)
                FReferences.Add(asm);
        }

        protected internal void AddType(ITypeInfo type) 
        {
            IGenericTypeInfo? genericType = type as IGenericTypeInfo;

            if (genericType?.IsGenericDefinition == true)
                return;

            if (!FTypes.Add(type)) // korkoros referencia fix
                return;

            IAssemblyInfo? asm = type.DeclaringAssembly;

            if (asm is not null)
            {
                if (asm.IsDynamic)
                    throw new NotSupportedException(Resources.DYNAMIC_ASM);

                FReferences.Add(asm);
            }

            //
            // Generikus parameterek szerepelhetnek masik szerelvenyben.
            //

            if (genericType is not null)
                foreach (ITypeInfo genericArg in genericType.GenericArguments)
                    AddType(genericArg);
  
            //
            // Az os (osztaly) szerepelhet masik szerelvenyben.
            //

            foreach (ITypeInfo @base in type.Bases)
                AddType(@base);

            //
            // "os" interface-ek szarmazhatnak masik szerelvenybol.
            //

            foreach (ITypeInfo iface in type.Interfaces)
                AddType(iface);
        }
    }
}