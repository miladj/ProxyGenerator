# ProxyGenerator
This a simple proxy generator written in C#.
It uses Dynamic class and ilgenerator to generate a near compile time class that can run as fast as possible.

## ASP.NET Core 
This a series of helper to help intercept and decorate service registered in the default aspnet core container.
You have 2 option to intercept calls 
1. implement `IInterceptor`
2. implement Decorator class

```C#
public interface ISimple<T>
{
        string Test();
}
public class Simple<T> : ISimple<T>
{
    public string Test()
    {
        return "OK";
    }
}
public interface IService { }
public class Service:IService{}
public class SimpleDecorator<T> : ISimple<T>
{
    private readonly ISimple<T> _original;
    private IService _service;

    public SimpleDecorator(ISimple<T> original,IService service)
    {
        _service = service;
        _original = original;
    }

    public string Test()
    {
        return _original.Test();
    }
}
public class PassThroughInterceptor : IInterceptor
{
    public virtual object Intercept(IInvocation invocation, Func<object> next)
    {
        return next();
    }
}
```
1. using interceptor
```C#
services.AddSingleton(typeof(ISimple<>), typeof(Simple<>));
services.AddTransient<PassThroughInterceptor>();
services.Intercept(typeof(ISimple<>), typeof(PassThroughInterceptor));
```
2. using decorator
```C#
services.AddTransient<IService, Service>();
services.AddTransient(typeof(ISimple<>), typeof(Simple<>));
services.Decorate(typeof(ISimple<>), typeof(SimpleDecorator<>));
```
## General purpose proxy generator

 There are two method to create proxy

1. this one help you create a proxy with two parameter constructor (typeToProxy,IInterceptor[] )
```C#
ProxyMaker.CreateProxyType(Type typeToProxy,params Type[] interceptorTypes)
```


2. this one help you create a proxy that resolve types from `IServicePrvider` that accept from constructor so this method create a constructor that accept `IServiceProvider`

```C#
ProxyMaker.CreateProxyTypeUseIServiceProvider(Type typeToProxy,params Type[] interceptorTypes)
```
***
## Interceptors
interceptors are called in a pipeline. and the action can be perform before and after the method.
***
## Benchmarks

1. call a method for a proxy generated object and an object generated by `new` keyword without any interceptor (just relay method calls)-[Benchmark](ProxyGenerator.Test/ProxyInstanceBenchmark.cs).

| Method | Mean | Error | StdDev |
| --- | --- | --- | --- |
| NonProxyCall | 2.653 ns | 0.0311 ns | 0.0291 ns |
| ProxyCall | 3.326 ns | 0.0727 ns | 0.0644 ns |
| WindsorProxyCall | 41.598 ns | 0.5268 ns | 0.4927 ns |

2. call a method for a proxy generated object and an object generated by `new` keyword with interceptors-[Benchmark](ProxyGenerator.Test/ProxyInterceptorBenchmark.cs).

| Method | Mean | Error | StdDev |
| --- | --- | --- | --- |
| NonProxyCall | 19.27 ns | 0.278 ns | 0.232 ns |
| ProxyCall | 59.72 ns | 0.513 ns | 0.455 ns |
| WindsOrProxyCall | 47.87 ns | 0.957 ns | 0.895 ns |



## Limitations
1. no support for generic constraints.
2. no support for ref, in, out method parameters.
3. no supoprt for changing arguments in interceptors.
4. no support for changing target in runtime.

## References
[Scrutor](https://github.com/khellang/Scrutor)

[Castle Core](https://github.com/castleproject/Core)