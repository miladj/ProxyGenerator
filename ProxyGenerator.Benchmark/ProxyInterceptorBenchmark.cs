using System;
using BenchmarkDotNet.Attributes;
using ProxyGenerator.Core;

namespace ProxyGenerator.Benchmark
{
    public class ProxyInterceptorBenchmark
    {
        private ITest _manualCreatedInstance;
        private ITest _generatedProxyInstance;
        private ITest _windsorGeneratedProxy;

        [GlobalSetup]
        public void Setup()
        {
            _manualCreatedInstance = new Proxy(new DefaultImpl(),new PassThroughInterceptor());
            _generatedProxyInstance = Activator.CreateInstance(ProxyMaker.CreateProxyType(typeof(ITest)),
                new DefaultImpl(), new IInterceptor[] {new PassThroughInterceptor()}) as ITest;
            _windsorGeneratedProxy = new Castle.DynamicProxy.ProxyGenerator().CreateInterfaceProxyWithTarget<ITest>(new DefaultImpl(),new WindsorPassThroughInterceptor());
            
        }
        public class PassThroughInterceptor : IInterceptor
        {
            public virtual object Intercept(IInvocation invocation, Func<object> next)
            {
                return next();
            }
        }
        public class WindsorPassThroughInterceptor : Castle.DynamicProxy.IInterceptor
        {
            public void Intercept(Castle.DynamicProxy.IInvocation invocation)
            {
                invocation.Proceed();
            }
        }
        public interface ITest
        {
            void Test();
        }
        public class DefaultImpl : ITest
        {
            public void Test()
            {

            }
        }
        public class Proxy : ITest
        {
            private readonly ITest _instance;
            private readonly IInterceptor _interceptor;
            
            public Proxy(ITest instance,IInterceptor interceptor)
            {
                _instance = instance;
                _interceptor = interceptor;
            }
            public void Test()
            {
                _interceptor.Intercept(null, () =>
                {
                    _instance.Test();
                    return null;
                });
            }
        }
        [Benchmark]
        public void CompileTimeProxyCall() => _manualCreatedInstance.Test();
        [Benchmark]
        public void ProxyCall() => _generatedProxyInstance.Test();
        [Benchmark]
        public void WindsorProxyCall() => _windsorGeneratedProxy.Test();
    }
}