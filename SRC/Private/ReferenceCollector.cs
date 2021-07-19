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

        private readonly HashSet<ITypeInfo> FTypes = new(ITypeInfoComparer.Instance);

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

            if (genericType?.IsGenericDefinition is true)
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
            // Az os (osztaly) szerepelhet masik szerelvenyben.
            //

            if (type.BaseType is not null)
                AddType(type.BaseType);

            //
            // Befoglalo tipus(ok) generikus parameterei szarmazhatnak masik szerelvenybol
            //

            if (type.EnclosingType is not null)
                AddType(type.EnclosingType);

            //
            // Generikus parameterek szerepelhetnek masik szerelvenyben.
            //

            if (genericType is not null)
                AddTypesFrom(genericType.GenericArguments);

            //
            // "os" interface-ek szarmazhatnak masik szerelvenybol.
            //

            AddTypesFrom(type.Interfaces);

            void AddTypesFrom(IEnumerable<ITypeInfo> types) 
            {
                foreach (ITypeInfo type in types)
                    AddType(type);
            }
        }
    }
}