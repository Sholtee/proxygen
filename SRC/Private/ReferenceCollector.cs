/********************************************************************************
* ReferenceCollector.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Proxy.Internals
{
    using Properties;

    internal class ReferenceCollector
    {
        private readonly HashSet<IAssemblyInfo> FReferences = new(IAssemblyInfoComparer.Instance);

        public IReadOnlyCollection<IAssemblyInfo> References => FReferences;

        private readonly HashSet<ITypeInfo> FTypes = new(ITypeInfoComparer.Instance);

        public IReadOnlyCollection<ITypeInfo> Types => FTypes;

        protected internal void AddType(ITypeInfo type) 
        {
            IGenericTypeInfo? genericType = type as IGenericTypeInfo;

            if (genericType?.IsGenericDefinition is true)
                return;

            if (!FTypes.Add(type)) // circular reference fix
                return;

            IAssemblyInfo? asm = type.DeclaringAssembly;

            if (asm is not null)
            {
                if (asm.IsDynamic)
                    throw new NotSupportedException(Resources.DYNAMIC_ASM);

                FReferences.Add(asm);
            }

            //
            // The base can be provided by a different assembly.
            //

            if (type.BaseType is not null)
                AddType(type.BaseType);

            //
            // The generic parameters of enclosing type(s) may come from a different assembly.
            //

            if (type.EnclosingType is not null)
                AddType(type.EnclosingType);

            //
            // Generic parameters may come from different assemblies.
            //

            if (genericType is not null)
                AddTypesFrom(genericType.GenericArguments);

            //
            // Interfaces may be also defined in a different assembly.
            //

            AddTypesFrom(type.Interfaces);

            void AddTypesFrom(IEnumerable<ITypeInfo> types) 
            {
                foreach (ITypeInfo type in types)
                {
                    AddType(type);
                }
            }
        }
    }
}