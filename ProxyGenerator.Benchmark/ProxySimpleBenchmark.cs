using System;
using BenchmarkDotNet.Attributes;
using ProxyGenerator.Core;

namespace ProxyGenerator.Benchmark
{
    public class ProxySimpleBenchmark
    {
        private ITest _manualCreatedInstance ;
        private ITest _generatedProxyInstance;

        [GlobalSetup]
        public void Setup()
        {
            _manualCreatedInstance = new Proxy(new DefaultImpl());
            _generatedProxyInstance = Activator.CreateInstance(new ProxyMaker(typeof(ITest)).CreateProxy(), new DefaultImpl()) as ITest;
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

            public Proxy(ITest instance)
            {
                _instance = instance;
            }
            public void Test()
            {
                _instance.Test();
            }
        }
        [Benchmark]
        public void NonProxyCall() => _manualCreatedInstance.Test();
        [Benchmark]
        public void ProxyCall() => _generatedProxyInstance.Test();
    }
}