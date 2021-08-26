using System;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using ProxyGenerator.Core;

namespace ProxyGenerator.Benchmark
{
    public class ProxyInstanceBenchmark
    {
        private Type _generatedProxyType;
        private Func<ITest> _expressionTreeConstruct;

        [GlobalSetup]
        public void Setup()
        {
            _generatedProxyType = new ProxyMaker(typeof(ITest)).CreateProxy();
            _expressionTreeConstruct = Expression.Lambda<Func<ITest>>(Expression.New(_generatedProxyType.GetConstructors()[0],
                Expression.New(typeof(DefaultImpl)))).Compile();
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
        public class CompileTimeProxy : ITest
        {
            private readonly ITest _instance;

            public CompileTimeProxy(ITest instance)
            {
                _instance = instance;
            }
            public void Test()
            {
                _instance.Test();
            }
        }
        [Benchmark]
        public void ManualCreateObject() => new CompileTimeProxy(new DefaultImpl()).Test();
        [Benchmark]
        public void ProxyInstantiateByActivator() => (Activator.CreateInstance(_generatedProxyType, new DefaultImpl()) as ITest)!.Test();
        [Benchmark]
        public void ProxyInstantiateByExpressionTree() => _expressionTreeConstruct().Test();
    }
}