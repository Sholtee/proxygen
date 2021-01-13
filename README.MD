# ProxyGen.NET [![Build status](https://ci.appveyor.com/api/projects/status/caw7qqtf5tbaa1fq/branch/master?svg=true)](https://ci.appveyor.com/project/Sholtee/proxygen/branch/master) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/proxygen/master) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/proxygen/badge.svg?branch=master)](https://coveralls.io/github/Sholtee/proxygen?branch=master) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/proxygen.net)](https://www.nuget.org/packages/proxygen.net)
> .NET proxy generator powered by Roslyn

**This documentation refers the version 4.X of the library**
## Purposes
This library currently supports generating [proxies](https://en.wikipedia.org/wiki/Proxy_pattern ) for interface interception and [duck typing](https://en.wikipedia.org/wiki/Duck_typing ).
### To hook into interface method calls:
1. Create the interceptor class (which is an [InterfaceInterceptor](https://sholtee.github.io/proxygen/doc/Solti.Utils.Proxy.InterfaceInterceptor-1.html ) descendant):
  ```csharp
  using Solti.Utils.Proxy;
  ...
  public class MyInterceptor: InterfaceInterceptor<IMyInterface>
  {
    public MyInterceptor(IMyInterface target) : base(target){}  // Nothing to do here

    public override object? Invoke(MethodInfo method, object?[] args, MemberInfo extra) // Invoked on every method call on generated proxy instance
    {
	  if (needToModifyRetval)
	  {
	    return something;
        // ref|out parameters can be assigned by setting the corresponding "args[]" item 
	  }
	  
	  args[0] = someNewVal; // "someNewVal" will be forwarded to the original method
	  
	  return base.Invoke(method, args, extra); // Let the original method do its work
    }  
  }
  ```
2. Generate the proxy type:
  ```csharp
  using Solti.Utils.Proxy.Generators;
  ...  
  // The returned type 
  // 1) is assembled only once
  // 2) "inherits" all the public constructors from "MyInterceptor"
  Type proxyType = ProxyGenerator<IMyInterface, MyInterceptor>.GetGeneratedType(); // or GetGeneratedTypeAsync()
  ```
3. Activate the proxy:
  ```csharp
  using System;
  ...
  IMyInterface target = ...;
  ...
  IMyInterface proxy = (IMyInterface) Activator.CreateInstance(proxyType, new object[]{target});
  ```
4. Enjoy

For further usage examples see [this](https://github.com/Sholtee/proxygen/blob/master/TEST/ProxyGen.Tests/Generators/ProxyGenerator.cs ) or [that](https://github.com/Sholtee/injector#decorating-services ).
### To create ducks:
1. Declare an interface that covers the desired members of the target class:
  ```csharp
  public class TargetClass // does not implement IDuck
  {
    public void Foo(){...}
  }
  ...
  public interface IDuck 
  {
    void Foo();
  }
  ```
2. Generate the duck type:
  ```csharp
  using Solti.Utils.Proxy.Generators;
  ...
  // The returned type:
  // 1) is assembled only once
  // 2) has only one public constructor with the following layout: "GeneratedProxy(TargetClass target)"
  Type duckType = DuckGenerator<IDuck, TargetClass>.GetGeneratedType(); // or GetGeneratedTypeAsync()
  ```
3. Quack:
  ```csharp
  using System;
  ...
  TargetClass target = ...;
  ...
  IDuck duck = (IDuck) Activator.CreateInstance(duckType, new object[]{target});
  ```
  
Related tests can be seen [here](https://github.com/Sholtee/proxygen/blob/master/TEST/ProxyGen.Tests/Generators/DuckGenerator.cs ).
## Caching the generated assembly
By setting the `configProperties.AssemblyCacheDir` property in `YourApp.runtimeconfig.json` you can make the system cache the generated assembly, so next time your app starts and requires the proxy there won't be time consuming emitting operation.

You can do it easily by creating a template file named `runtimeconfig.template.json` in your project folder:
```json
{
  "configProperties": {
    "AssemblyCacheDir": "GeneratedAssemblies"
  }
}
```
## Embedding the generated type
This library can be used as a [source generator](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/ ) so you can embed the generated proxy type into the assembly that uses it. Since all the time consumig operations happen in compile time this feature can singificantly improve the performance. To enable:

1. Reference this library as a source generator: *TODO*
2. Use the `Solti.Utils.Proxy.Attributes.EmbedGeneratedTypeAttribute` to select proxies to be embedded: `[assembly: EmbedGeneratedType(typeof(ProxyGenerator<IMyInterface, MyInterceptor<IMyInterface>>))]` or `[assembly: EmbedGeneratedType(typeof(DuckGenerator<IMyInterface, MyClass>))]`

Note that open generics are not supported.
## Inspecting the generated code
*ProxyGen* is able to dump the generated sources. Due to performance considerations it is disabled by default. To enable, set the `configProperties.SourceDump` property to the desired directory (note that environment variables are supported):
```json
{
  "configProperties": {
    "AssemblyCacheDir": "%TEMP%"
  }
}
```
## Migrating from version 
- 2.X
  - Delete all the cached assemblies (if the `[Proxy|Duck]Generator.CacheDirectory` is set somewhere)
  - `InterfaceInterceptor.Invoke()` returns the result of the original method (instead of `CALL_TARGET`) so in the override you may never need to invoke the `method` parameter directly.
- 3.X
  - `[Proxy|Duck]Generator.GeneratedType[Async]` property has been removed. To get the generated proxy type call the `[Proxy|Duck]Generator.GetGeneratedType[Async]()` method.
  - `[Proxy|Duck]Generator.CacheDirectory` property has been removed. To set the cache directory tweak the [runtimeconfig.json](#caching-the-generated-assembly) file.
## Resources
- [API Docs](https://sholtee.github.io/proxygen ) (WIP).
- [Benchmark Results](https://sholtee.github.io/proxygen/perf )
- [Version History](https://github.com/Sholtee/proxygen/blob/master/history.md )

## Supported frameworks
This project currently targets *.NET Standard* 2.0 and 2.1.