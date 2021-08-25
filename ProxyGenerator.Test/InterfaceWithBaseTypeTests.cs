using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            ProxyMaker proxyMaker = new ProxyMaker(typeof(IChild));
            Type proxy = proxyMaker.CreateProxy();
            Mock<IChild> mock = new Mock<IChild>();
            IChild instance = Activator.CreateInstance(proxy,mock.Object) as IChild;
            instance.Test();
            mock.Verify(x=>x.Test());
        }
        [Test]
        public void TestIChild2()
        {
            ProxyMaker proxyMaker = new ProxyMaker(typeof(IChild2));
            Type proxy = proxyMaker.CreateProxy();
            Mock<IChild2> mock = new Mock<IChild2>();
            IChild2 instance = Activator.CreateInstance(proxy, mock.Object) as IChild2;
            instance.Test();
            mock.Verify(x => x.Test());
        }
        [Test]
        public void TestAbstractClassBase()
        {
            ProxyMaker proxyMaker = new ProxyMaker(typeof(AbstractClassBase<>));

            Type proxy = proxyMaker.CreateProxy();
            Mock<AbstractClassBase<string>> mock = new Mock<AbstractClassBase<string>>();
            mock.Setup(x => x.Test(10)).Returns("OK");
            AbstractClassBase<string> instance = Activator.CreateInstance(proxy.MakeGenericType(typeof(string)), mock.Object) as AbstractClassBase<string>;
            var actualRv=instance.Test(10);
            Assert.AreEqual(actualRv,"OK");
            
        }
        [Test]
        public void TestAbstractClassChild()
        {
            ProxyMaker proxyMaker = new ProxyMaker(typeof(AbstractClassChild<>));

            Type proxy = proxyMaker.CreateProxy();
            Mock<AbstractClassChild<int>> mock = new Mock<AbstractClassChild<int>>();
            mock.Setup(x => x.Test(10,100)).Returns("OK");
            AbstractClassChild<int> instance = Activator.CreateInstance(proxy.MakeGenericType(typeof(int)), mock.Object) as AbstractClassChild<int>;
            var actualRv = instance.Test(10,100);
            Assert.AreEqual(actualRv, "OK");

        }
    }
}
