using System;
using BenchmarkDotNet.Attributes;
using ProxyGenerator.Core;

namespace ProxyGenerator.Benchmark
{
    public class ProxyInterceptorBenchmark
    {
        private ITest _manualCreatedInstance;
        private ITest _generatedProxyInstance;

        [GlobalSetup]
        public void Setup()
        {
            _manualCreatedInstance = new Proxy(new DefaultImpl(),new PassThoughInterceptor());
            _generatedProxyInstance = Activator.CreateInstance(ProxyMaker.CreateProxyType(typeof(ITest)),
                new DefaultImpl(), new IInterceptor[] {new PassThoughInterceptor()}) as ITest;
        }
        public class PassThoughInterceptor : IInterceptor
        {
            public virtual object Intercept(IInvocation invocation, Func<object> next)
            {
                return next();
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
        public void NonProxyCall() => _manualCreatedInstance.Test();
        [Benchmark]
        public void ProxyCall() => _generatedProxyInstance.Test();
    }
}