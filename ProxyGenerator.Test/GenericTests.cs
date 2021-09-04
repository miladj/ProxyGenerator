using System;
using System.Reflection;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;

namespace ProxyGenerator.Test
{
    public class GenericTests
    {
        public interface IGeneric<T>
        {
            void Test();
        }
        public interface IGeneric2<T>
        {
            void Test(T value);

        }
        public interface IGeneric3<T>
        {
            T Test();

        }
        public interface IGeneric4<T>
        {
            
            void Test<TM>();
        }

        public interface IGeneric5<T>
        {
            T Test<TM>(T interfaceGeneric);
        }
        public interface IGeneric6<T>
        {
            TM Test<TM>(TM methodGeneric, T interfaceGeneric);
        }
        public interface IGeneric7<T>
        {
            void Test<TM>(TM values);
        }
        [Test]
        public void VoidZeroParameterMethod()
        {
            var mock = new Moq.Mock<IGeneric<string>>();
            Type proxy = new Core.ProxyMaker(typeof(IGeneric<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, new []{new PassThroughInterceptor()}) as IGeneric<string>;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test();
            mock.Verify(x => x.Test());
        }
        [Test]
        public void VoidReturnGenericParameterMethod()
        {
            var mock = new Moq.Mock<IGeneric2<string>>();
            Type proxy = new Core.ProxyMaker(typeof(IGeneric2<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, new []{new PassThroughInterceptor()}) as IGeneric2<string>;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test("Hello");
            mock.Verify(x => x.Test("Hello"));
        }
        [Test]
        public void GenericReturnNoParameterMethod()
        {
            var mock = new Moq.Mock<IGeneric3<string>>();
            const string expectedRv = "GenericReturn";
            mock.Setup(x => x.Test()).Returns(expectedRv);
            Type proxy = new Core.ProxyMaker(typeof(IGeneric3<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, new []{new PassThroughInterceptor()}) as IGeneric3<string>;
            Assert.NotNull(createdObjectFromProxy);
            string actualRv = createdObjectFromProxy.Test();
            Assert.AreEqual(expectedRv,actualRv);
        }
        [Test]
        public void IGeneric4_Test()
        {
            var k = typeof(IGeneric4<string>);
            var mock = new Moq.Mock<IGeneric4<string>>();


            Type proxy = new Core.ProxyMaker(typeof(IGeneric4<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, Array.Empty<IInterceptor>()) as IGeneric4<string>;

            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test<string>();
            mock.Verify(x => x.Test<string>());
        }
        [Test]
        public void IGeneric4_Interceptor_Test()
        {
            var k= typeof(IGeneric4<string>);
            var mock = new Moq.Mock<IGeneric4<string>>();
            
            
            Type proxy = new Core.ProxyMaker(typeof(IGeneric4<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, new []{new PassThroughInterceptor()}) as IGeneric4<string>;
            
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test<string>();
            mock.Verify(x => x.Test<string>());
        }
        [Test]
        public void GenericMethod2_Test()
        {
            var mock = new Moq.Mock<IGeneric5<string>>();
            mock.Setup(x => x.Test<int>("Hello")).Returns("Hello");

            Type proxy = new Core.ProxyMaker(typeof(IGeneric5<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, new []{new PassThroughInterceptor()}) as IGeneric5<string>;
            Assert.NotNull(createdObjectFromProxy);
            Assert.AreEqual("Hello",createdObjectFromProxy.Test<int>("Hello"));
        }
        [Test]
        public void IGeneric6_Test()
        {
            var mock = new Moq.Mock<IGeneric6<string>>();

            mock.Setup(x => x.Test(10, "Hello")).Returns(1000);
            Type proxy = new Core.ProxyMaker(typeof(IGeneric6<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object,Array.Empty<IInterceptor>()/*, new []{new InterceptorTests.M()}*/) as IGeneric6<string>;
            Assert.NotNull(createdObjectFromProxy);
            int genericMethod3 = createdObjectFromProxy.Test<int>(10,"Hello");
            Assert.AreEqual(1000,genericMethod3);
        }
        [Test]
        public void IGeneric6_Interceptor_Test()
        {
            var mock = new Moq.Mock<IGeneric6<string>>();

            mock.Setup(x => x.Test(10, "Hello")).Returns(1000);
            Type proxy = new Core.ProxyMaker(typeof(IGeneric6<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, new []{new PassThroughInterceptor()}) as IGeneric6<string>;
            Assert.NotNull(createdObjectFromProxy);
            int genericMethod3 = createdObjectFromProxy.Test<int>(10, "Hello");
            Assert.AreEqual(1000, genericMethod3);
        }
        [Test]
        public void IGeneric7_Test()
        {
            var mock = new Moq.Mock<IGeneric7<string>>();

            
            Type proxy = new Core.ProxyMaker(typeof(IGeneric7<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, Array.Empty<IInterceptor>()) as IGeneric7<string>;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test( "Hello");
            mock.Verify(x=>x.Test(It.IsAny<string>()));
        }
        [Test]
        public void IGeneric7_Interceptor_Test()
        {
            var mock = new Moq.Mock<IGeneric7<string>>();


            Type proxy = new Core.ProxyMaker(typeof(IGeneric7<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, new[] { new PassThroughInterceptor() }) as IGeneric7<string>;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test("Hello");
            mock.Verify(x => x.Test(It.IsAny<string>()));
        }
    }
}