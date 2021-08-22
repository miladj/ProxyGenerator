using System;
using Moq;
using NUnit.Framework;

namespace ProxyGenerator.Test
{
    public class GenericTests
    {
        public interface IGeneric<T>
        {
            void Test();
            void Test(T value);
            T Test2();
            void GenericMethod<TM>();
            T GenericMethod2<TM>(T interfaceGeneric);
            TM GenericMethod3<TM>(TM methodGeneric, T interfaceGeneric);
            
        }
        
        
        [Test]
        public void VoidZeroParameterMethod()
        {
            var mock = new Moq.Mock<IGeneric<string>>();
            Type proxy = new Core.ProxyMaker(typeof(IGeneric<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object) as IGeneric<string>;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test();
            mock.Verify(x => x.Test());
        }
        [Test]
        public void VoidReturnGenericParameterMethod()
        {
            var mock = new Moq.Mock<IGeneric<string>>();
            Type proxy = new Core.ProxyMaker(typeof(IGeneric<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object) as IGeneric<string>;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.Test("Hello");
            mock.Verify(x => x.Test("Hello"));
        }
        [Test]
        public void GenericReturnNoParameterMethod()
        {
            var mock = new Moq.Mock<IGeneric<string>>();
            const string expectedRv = "GenericReturn";
            mock.Setup(x => x.Test2()).Returns(expectedRv);
            Type proxy = new Core.ProxyMaker(typeof(IGeneric<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object) as IGeneric<string>;
            Assert.NotNull(createdObjectFromProxy);
            string actualRv = createdObjectFromProxy.Test2();
            Assert.AreEqual(expectedRv,actualRv);
        }
        [Test]
        public void GenericMethodVoidReturnNoParameterMethod()
        {
            var mock = new Moq.Mock<IGeneric<string>>();
            
            
            Type proxy = new Core.ProxyMaker(typeof(IGeneric<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object) as IGeneric<string>;
            Assert.NotNull(createdObjectFromProxy);
            createdObjectFromProxy.GenericMethod<string>();
            mock.Verify(x => x.GenericMethod<string>());
        }
        [Test]
        public void GenericMethod2_Test()
        {
            var mock = new Moq.Mock<IGeneric<string>>();
            mock.Setup(x => x.GenericMethod2<int>("Hello")).Returns("Hello");

            Type proxy = new Core.ProxyMaker(typeof(IGeneric<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object) as IGeneric<string>;
            Assert.NotNull(createdObjectFromProxy);
            Assert.AreEqual("Hello",createdObjectFromProxy.GenericMethod2<int>("Hello"));
        }
        [Test]
        public void GenericMethod3_Test()
        {
            var mock = new Moq.Mock<IGeneric<string>>();

            mock.Setup(x => x.GenericMethod3(10, "Hello")).Returns(1000);
            Type proxy = new Core.ProxyMaker(typeof(IGeneric<>)).CreateProxy();
            var createdObjectFromProxy = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object) as IGeneric<string>;
            Assert.NotNull(createdObjectFromProxy);
            int genericMethod3 = createdObjectFromProxy.GenericMethod3<int>(10,"Hello");
            Assert.AreEqual(1000,genericMethod3);
        }
    }
}