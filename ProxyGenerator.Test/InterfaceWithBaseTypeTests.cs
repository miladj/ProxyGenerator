using System;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;

namespace ProxyGenerator.Test
{
    public class InterfaceWithBaseTypeTests
    {
        public interface IBase
        {
            void Test();
        }
        public interface IChild:IBase
        {
            
        }
        public interface IChild2:IChild
        {
            
        }
        public abstract class AbstractClassBase<T>
        {
            public abstract T Test(int i);
        }

        public abstract class AbstractClassChild<T> : AbstractClassBase<T>
        {
            public abstract string Test(int a, T b);
        }
        [Test]
        public void TestIChild()
        {
            Type proxy = ProxyMaker.CreateProxyType(typeof(IChild));
            Mock<IChild> mock = new Mock<IChild>();
            IChild instance = Activator.CreateInstance(proxy,mock.Object, Array.Empty<IInterceptor>()) as IChild;
            instance.Test();
            mock.Verify(x=>x.Test());
        }
        [Test]
        public void TestIChild2()
        {
            Type proxy = ProxyMaker.CreateProxyType(typeof(IChild2));
            
            Mock<IChild2> mock = new Mock<IChild2>();
            IChild2 instance = Activator.CreateInstance(proxy, mock.Object, Array.Empty<IInterceptor>()) as IChild2;
            instance.Test();
            mock.Verify(x => x.Test());
        }
        [Test]
        public void TestIChild2_Interceptor()
        {
            Type proxy = ProxyMaker.CreateProxyType(typeof(IChild2));
            
            Mock<IChild2> mock = new Mock<IChild2>();
            IChild2 instance = Activator.CreateInstance(proxy, mock.Object, new IInterceptor[]{new PassThroughInterceptor()}) as IChild2;
            instance.Test();
            mock.Verify(x => x.Test());
        }
        [Test]
        public void TestAbstractClassBase()
        {
            Type proxy = ProxyMaker.CreateProxyType(typeof(AbstractClassBase<>));

            
            Mock<AbstractClassBase<string>> mock = new Mock<AbstractClassBase<string>>();
            mock.Setup(x => x.Test(10)).Returns("OK");
            AbstractClassBase<string> instance = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, Array.Empty<IInterceptor>()) as AbstractClassBase<string>;
            var actualRv=instance.Test(10);
            Assert.AreEqual(actualRv,"OK");
            
        }
        [Test]
        public void TestAbstractClassBase_Interceptor()
        {
            Type proxy = ProxyMaker.CreateProxyType(typeof(AbstractClassBase<>));

            
            Mock<AbstractClassBase<string>> mock = new Mock<AbstractClassBase<string>>();
            mock.Setup(x => x.Test(10)).Returns("OK");
            AbstractClassBase<string> instance = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object, new IInterceptor[]{new PassThroughInterceptor()}) as AbstractClassBase<string>;
            var actualRv = instance.Test(10);
            Assert.AreEqual(actualRv, "OK");

        }
        [Test]
        public void TestAbstractClassChild()
        {
            Type proxy = ProxyMaker.CreateProxyType(typeof(AbstractClassChild<>));
            Mock<AbstractClassChild<int>> mock = new Mock<AbstractClassChild<int>>();
            mock.Setup(x => x.Test(10,100)).Returns("OK");
            AbstractClassChild<int> instance = Activator.CreateInstance(proxy.MakeGenericType(typeof(int)), mock.Object,Array.Empty<IInterceptor>()) as AbstractClassChild<int>;
            var actualRv = instance.Test(10,100);
            Assert.AreEqual(actualRv, "OK");
        }
        [Test]
        public void TestAbstractClassChild_Interceptor()
        {

            Type proxy = ProxyMaker.CreateProxyType(typeof(AbstractClassChild<>));

            Mock<AbstractClassChild<int>> mock = new Mock<AbstractClassChild<int>>();
            mock.Setup(x => x.Test(10, 100)).Returns("OK");
            AbstractClassChild<int> instance = Activator.CreateInstance(proxy.MakeGenericType(typeof(int)), mock.Object, new IInterceptor[]{new PassThroughInterceptor()}) as AbstractClassChild<int>;
            var actualRv = instance.Test(10, 100);
            Assert.AreEqual(actualRv, "OK");

        }
    }
}
