/********************************************************************************
* SyntaxFactoryBase.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.Proxy.Internals
{
    using Abstractions;

    internal partial class SyntaxFactoryBase : ISyntaxFactory
    {
        private (CompilationUnitSyntax Unit, IReadOnlyCollection<MetadataReference> References, IReadOnlyCollection<Type> Types)? FContext;

        protected virtual CompilationUnitSyntax GenerateProxyUnit(CancellationToken cancellation) => throw new NotImplementedException();

        public virtual string? AssemblyName { get; }

        public (CompilationUnitSyntax Unit, IReadOnlyCollection<MetadataReference> References, IReadOnlyCollection<Type> Types) GetContext(CancellationToken cancellation = default) =>  FContext ??=
        (
            GenerateProxyUnit(cancellation),
            FReferences
                .Select(asm => MetadataReference.CreateFromFile(asm.Location))
                .ToArray(),
            FTypes
        );
    }
}