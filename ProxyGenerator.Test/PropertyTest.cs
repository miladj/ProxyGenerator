using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;

namespace ProxyGenerator.Test
{
    public class PropertyTest
    {
        public interface IProperty
        {
            string SProp { get; set; }
        }
        [Test]
        public void PropertyGetTest()
        {
            Mock<IProperty> mock = new Mock<IProperty>();
            const string expectedValue = "100000";
            mock.SetupProperty(x => x.SProp, expectedValue);
            var proxyType = new ProxyMaker(typeof(IProperty)).CreateProxy();
            var proxiedObject=Activator.CreateInstance(proxyType, mock.Object, Array.Empty<IInterceptor>()) as IProperty;
            Assert.AreEqual(expectedValue, proxiedObject.SProp);

        }
        [Test]
        public void PropertyGetTest_Interceptor()
        {
            Mock<IProperty> mock = new Mock<IProperty>();
            const string expectedValue = "100000";
            mock.SetupProperty(x => x.SProp, expectedValue);
            var proxyType = new ProxyMaker(typeof(IProperty)).CreateProxy();
            var proxiedObject = Activator.CreateInstance(proxyType, mock.Object, new IInterceptor[]{new PassThroughInterceptor()}) as IProperty;
            Assert.AreEqual(expectedValue, proxiedObject.SProp);

        }
        [Test]
        public void PropertySetTest()
        {
            Mock<IProperty> mock = new Mock<IProperty>();
            const string expectedValue = "100000";
            mock.SetupProperty(x => x.SProp);
            var proxyType = new ProxyMaker(typeof(IProperty)).CreateProxy();
            var proxiedObject = Activator.CreateInstance(proxyType, mock.Object, Array.Empty<IInterceptor>()) as IProperty;
            proxiedObject.SProp = expectedValue;
            Assert.AreEqual(expectedValue, mock.Object.SProp);

        }
        [Test]
        public void PropertySetTest_Interceptor()
        {
            Mock<IProperty> mock = new Mock<IProperty>();
            const string expectedValue = "100000";
            mock.SetupProperty(x => x.SProp);
            var proxyType = new ProxyMaker(typeof(IProperty)).CreateProxy();
            var proxiedObject = Activator.CreateInstance(proxyType, mock.Object, new IInterceptor[] { new PassThroughInterceptor() }) as IProperty;
            proxiedObject.SProp = expectedValue;
            Assert.AreEqual(expectedValue, mock.Object.SProp);

        }
    }
}
