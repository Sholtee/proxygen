/********************************************************************************
* MetadataTypeInfo.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.Proxy.Internals
{
    internal class MetadataTypeInfo : ITypeInfo
    {
        protected Type UnderlyingType { get; }

        protected MetadataTypeInfo(Type underlyingType) => UnderlyingType = underlyingType;

        public static ITypeInfo CreateFrom(Type underlyingType) => underlyingType switch
        {
            _ when underlyingType.IsArray => new MetadataArrayTypeInfo(underlyingType),
            _ when underlyingType.GetOwnGenericArguments().Any() => new MetadataGenericTypeInfo(underlyingType),
            _ => new MetadataTypeInfo(underlyingType)
        };

        public override bool Equals(object obj) => obj is MetadataTypeInfo self && UnderlyingType.Equals(self.UnderlyingType);

        public override int GetHashCode() => UnderlyingType.GetHashCode();

        private IAssemblyInfo? FDeclaringAssembly;
        public IAssemblyInfo DeclaringAssembly => FDeclaringAssembly ??= MetadataAssemblyInfo.CreateFrom(UnderlyingType.Assembly);

        public bool IsVoid => UnderlyingType == typeof(void);

        private IReadOnlyList<ITypeInfo>? FEnclosingTypes;
        public IReadOnlyList<ITypeInfo> EnclosingTypes => FEnclosingTypes ??= UnderlyingType
            .GetEnclosingTypes()
            .Select(CreateFrom)
            .ToArray();

        private IReadOnlyList<ITypeInfo>? FInterfaces;
        public IReadOnlyList<ITypeInfo> Interfaces => FInterfaces ??= UnderlyingType
            .GetInterfaces()
            .Select(CreateFrom)
            .ToArray();

        private IReadOnlyList<ITypeInfo>? FBases;
        public IReadOnlyList<ITypeInfo> Bases => FBases ??= UnderlyingType
            .GetBaseTypes()
            .Select(CreateFrom)
            .ToArray();

        public virtual string Name => UnderlyingType.GetFriendlyName();

        public bool IsByRef => UnderlyingType.IsByRef;

        private ITypeInfo? FElementType;
        public ITypeInfo? ElementType 
        {
            get 
            {
                if (FElementType == null)
                {
                    Type? realType = UnderlyingType.GetElementType();

                    if (realType != null)
                        FElementType = CreateFrom(realType);
                }
                return FElementType;
            }
        }

        //
        // "Cica<T>.Mica<TT>"-nal a "TT" is beagyazott ami nekunk nem jo
        //

        public bool IsNested => UnderlyingType.IsNested && !UnderlyingType.IsGenericParameter;

        public bool IsInterface => UnderlyingType.IsInterface;

        private IReadOnlyList<IPropertyInfo>? FProperties;
        public IReadOnlyList<IPropertyInfo> Properties => FProperties ??= UnderlyingType
            .ListMembers<PropertyInfo>(includeNonPublic: true /*explicit*/, includeStatic: true)
            .Select(MetadataPropertyInfo.CreateFrom)
            .ToArray();

        private IReadOnlyList<IEventInfo>? FEvents;
        public IReadOnlyList<IEventInfo> Events => FEvents ??= UnderlyingType
            .ListMembers<EventInfo>(includeNonPublic: true /*explicit*/, includeStatic: true)
            .Select(MetadataEventInfo.CreateFrom)
            .ToArray();

        private IReadOnlyList<IMethodInfo>? FMethods;
        public IReadOnlyList<IMethodInfo> Methods => FMethods ??= UnderlyingType
            .ListMembers<MethodInfo>(includeNonPublic: true /*explicit*/, includeStatic: true)
            .Select(MetadataMethodInfo.CreateFrom)
            .ToArray();

        private IReadOnlyList<IConstructorInfo>? FConstructors;
        public IReadOnlyList<IConstructorInfo> Constructors => FConstructors ??= UnderlyingType
            .GetPublicConstructors()
            .Select(MetadataMethodInfo.CreateFrom)
            .Cast<IConstructorInfo>()
            .ToArray();

        public string? AssemblyQualifiedName => UnderlyingType.AssemblyQualifiedName;

        private sealed class MetadataGenericTypeInfo : MetadataTypeInfo, IGenericTypeInfo
        {
            public MetadataGenericTypeInfo(Type underlyingType) : base(underlyingType) { }

            public bool IsGenericDefinition => UnderlyingType.IsGenericTypeDefinition;

            public IReadOnlyList<ITypeInfo> GenericArguments => UnderlyingType
                .GetOwnGenericArguments()
                .Select(CreateFrom)
                .ToArray();

            public override string Name => !UnderlyingType.IsGenericType || UnderlyingType.IsGenericTypeDefinition // FIXME: Type.GetFriendlyName() lezart generikusokat nem eszi meg (igaz elvileg nem is kell hivjuk lezart generikusra)
                ? base.Name
                : GenericDefinition.Name;

            public IGenericTypeInfo GenericDefinition => new MetadataGenericTypeInfo(UnderlyingType.GetGenericTypeDefinition());

            IGeneric IGeneric.GenericDefinition => GenericDefinition;

            public IGeneric Close(params ITypeInfo[] genericArgs) => new MetadataGenericTypeInfo
            (
                UnderlyingType.MakeGenericType
                (
                    genericArgs
                        .Select(arg => Type.GetType(arg.AssemblyQualifiedName, throwOnError: true))
                        .ToArray()
                )
            );
        }

        private sealed class MetadataArrayTypeInfo : MetadataTypeInfo, IArrayTypeInfo 
        {
            public MetadataArrayTypeInfo(Type underlyingType) : base(underlyingType) { }

            public int Rank => UnderlyingType.GetArrayRank();
        }
    }
}
