/********************************************************************************
* MetadataTypeInfo.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataTypeInfo(Type underlyingType) : ITypeInfo
    {
        protected Type UnderlyingType { get; } = underlyingType;

        public static ITypeInfo CreateFrom(Type underlyingType)
        {
            while (underlyingType.IsByRef)
                underlyingType = underlyingType.GetElementType();

            if (underlyingType.IsFunctionPointer())  // TODO: FIXME: remove this workaround
                underlyingType = typeof(IntPtr);

            return underlyingType switch
            {
                _ when underlyingType.IsArray => new MetadataArrayTypeInfo(underlyingType),
                _ when underlyingType.GetOwnGenericArguments().Any() => new MetadataGenericTypeInfo(underlyingType),
                _ => new MetadataTypeInfo(underlyingType)
            };
        }

        public override bool Equals(object obj) => obj is MetadataTypeInfo that && UnderlyingType.Equals(that.UnderlyingType);

        public override int GetHashCode() => UnderlyingType.GetHashCode();

        public override string ToString() => UnderlyingType.ToString();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IAssemblyInfo? FDeclaringAssembly;
        public IAssemblyInfo DeclaringAssembly => FDeclaringAssembly ??= MetadataAssemblyInfo.CreateFrom(UnderlyingType.Assembly);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<ITypeInfo?> FEnclosingType = new(() =>
        {
            Type? enclosingType = underlyingType.GetEnclosingType();

            return enclosingType is not null
                ? CreateFrom(enclosingType)
                : null;
        });
        public ITypeInfo? EnclosingType => FEnclosingType.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingType
            .GetAllInterfaces()
            .Select(CreateFrom)
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ITypeInfo? FBaseType;
        public ITypeInfo? BaseType => UnderlyingType.GetBaseType() is not null
            ? FBaseType ??= CreateFrom(UnderlyingType.GetBaseType()!)
            : null;

        public virtual string Name => UnderlyingType.GetFriendlyName();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<TypeInfoFlags> FFlags = new(() =>
        {
            TypeInfoFlags flags = TypeInfoFlags.None;

            if (underlyingType == typeof(void))
                flags |= TypeInfoFlags.IsVoid;

            if (underlyingType.IsDelegate())
                flags |= TypeInfoFlags.IsDelegate;

            if ((underlyingType.GetInnermostElementType() ?? underlyingType).IsGenericParameter)
                flags |= TypeInfoFlags.IsGenericParameter;

            if (underlyingType.IsNested() && !flags.HasFlag(TypeInfoFlags.IsGenericParameter))
                flags |= TypeInfoFlags.IsNested;

            if (underlyingType.IsInterface)
                flags |= TypeInfoFlags.IsInterface;

            if (underlyingType.IsClass())
                flags |= TypeInfoFlags.IsClass;

            if (underlyingType.IsSealed)
                flags |= TypeInfoFlags.IsFinal;
                
            if (underlyingType.IsAbstract())
                flags |= TypeInfoFlags.IsAbstract;

            return flags;
        });
        public TypeInfoFlags Flags => FFlags.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RefType? FRefType;
        public RefType RefType => FRefType ??= UnderlyingType.GetRefType();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<ITypeInfo?> FElementType = new(() =>
        {
            Type? realType = underlyingType.GetElementType();

            return realType is not null
                ? CreateFrom(realType)
                : null;
        }) ;
        public ITypeInfo? ElementType => FElementType.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IPropertyInfo>? FProperties;
        public IReadOnlyList<IPropertyInfo> Properties => FProperties ??= UnderlyingType
            .ListProperties(includeStatic: true)
            .Select(MetadataPropertyInfo.CreateFrom)
            .Sort()
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IEventInfo>? FEvents;
        public IReadOnlyList<IEventInfo> Events => FEvents ??= UnderlyingType
            .ListEvents(includeStatic: true)
            .Select(MetadataEventInfo.CreateFrom)
            .Sort()
            .ToImmutableList();

        //
        // These methods are generated by the compiler
        //

        private static bool ShouldSkip(MethodInfo m) =>
            (m.DeclaringType.IsClass && m.Name == "Finalize") ||  // destructor
            (m.DeclaringType.IsArray && m.Name == "Get") ||  // = array[i]
            (m.DeclaringType.IsArray && m.Name == "Set") ||  // array[i] =
            (m.DeclaringType.IsArray && m.Name == "Address");  // = ref array[i]

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IMethodInfo>? FMethods;
        public IReadOnlyList<IMethodInfo> Methods => FMethods ??= UnderlyingType
            .ListMethods(includeStatic: true)
            .Where(static meth => !ShouldSkip(meth))
            .Select(MetadataMethodInfo.CreateFrom)
            .Sort()
            .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IReadOnlyList<IConstructorInfo>? FConstructors;
        public IReadOnlyList<IConstructorInfo> Constructors => FConstructors ??= UnderlyingType
                .GetDeclaredConstructors()
                .Select(static ctor => (IConstructorInfo) MetadataMethodInfo.CreateFrom(ctor))
                .Sort()
                .ToImmutableList();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<string?> FAssemblyQualifiedName = new
        (
            () =>
            {
                string? qualifiedName = underlyingType.GetQualifiedName();
                return qualifiedName is not null
                    ? $"{qualifiedName}, {underlyingType.Assembly}"
                    : null;
            }
        );
        public string? AssemblyQualifiedName => FAssemblyQualifiedName.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<string?> FQualifiedName = new(() => underlyingType.GetQualifiedName());
        public string? QualifiedName => FQualifiedName.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Lazy<IHasName?> FContainingMember = new(() =>
        {
            Type concreteType = underlyingType.GetInnermostElementType() ?? underlyingType;

            return concreteType switch
            {
                _ when concreteType.IsGenericParameter && concreteType.DeclaringMethod is not null => MetadataMethodInfo.CreateFrom(concreteType.DeclaringMethod),
                _ when concreteType.GetEnclosingType() is not null => MetadataTypeInfo.CreateFrom(concreteType.GetEnclosingType()!),
                _ => null
            };
        });
        public IHasName? ContainingMember => FContainingMember.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AccessModifiers? FAccessModifiers;
        public AccessModifiers AccessModifiers => FAccessModifiers ??= UnderlyingType.GetAccessModifiers();

        private sealed class MetadataGenericTypeInfo(Type underlyingType) : MetadataTypeInfo(underlyingType), IGenericTypeInfo
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private bool? FIsGenericDefinition;
            public bool IsGenericDefinition => FIsGenericDefinition ??= UnderlyingType
                .GetGenericArguments()
                .Any(static ga => ga.IsGenericParameter);

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IReadOnlyList<ITypeInfo>? FGenericArguments;
            public IReadOnlyList<ITypeInfo> GenericArguments => FGenericArguments ??= UnderlyingType
                .GetOwnGenericArguments()
                .Select(CreateFrom)
                .ToImmutableList();

            public override string Name => !UnderlyingType.IsGenericType || UnderlyingType.IsGenericTypeDefinition // FIXME: Type.GetFriendlyName() doesn't handle closed generics
                ? base.Name
                : GenericDefinition.Name;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private IGenericTypeInfo? FGenericDefinition;
            public IGenericTypeInfo GenericDefinition => FGenericDefinition ??= new MetadataGenericTypeInfo(UnderlyingType.GetGenericTypeDefinition());

            public IReadOnlyList<IGenericConstraint> GenericConstraints =>
                //
                // We never generate open generic proxies so implementing this property not required
                //

                throw new NotImplementedException();

            public IGenericTypeInfo Close(params ITypeInfo[] genericArgs)
            {
                if (UnderlyingType.IsNested)
                    throw new NotImplementedException(); // TODO

                Type[] gas = new Type[genericArgs.Length];

                for (int i = 0; i < genericArgs.Length; i++)
                {
                    gas[i] = genericArgs[i].ToMetadata();
                }

                return (IGenericTypeInfo) CreateFrom(UnderlyingType.MakeGenericType(gas));
            }
        }

        private sealed class MetadataArrayTypeInfo(Type underlyingType) : MetadataTypeInfo(underlyingType), IArrayTypeInfo 
        {
            public int Rank => UnderlyingType.GetArrayRank();
        }
    }
}
