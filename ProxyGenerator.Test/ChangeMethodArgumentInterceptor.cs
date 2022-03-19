using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using IInvocation = ProxyGenerator.Core.IInvocation;


namespace ProxyGenerator.Test
{
    public class ChangeMethodArgumentInterceptor
    {
        public interface ITestMethod
        {
            string TestMethod(int id);
        }
        public interface ITestMethod2
        {
            string TestMethod(string original,int newInt);
        }

        [Test]
        public void TestChangeMethodArgumentInterceptor_ValueType()
        {
            const string expectedRv = "100";
            Mock<ITestMethod> mock1 = new Mock<ITestMethod>();
            mock1.Setup(x => x.TestMethod(It.IsAny<int>())).Returns((int x) => x.ToString());

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change first argument in interceptor
                    invocation.SetArgument(0,100);
                    return next();
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as ITestMethod;

            var actualRv = instance!.TestMethod(90);

            Assert.AreEqual(expectedRv, actualRv);
            mock1.Verify();
            mockInterceptor.VerifyAll();

        }
        [Test]
        public void TestChangeMethodArgumentInterceptor_RefType()
        {
            const string expectedRv = "Altered String 50";
            Mock<ITestMethod2> mock1 = new Mock<ITestMethod2>();
            mock1.Setup(x => x.TestMethod(It.IsAny<string>(),It.IsAny<int>())).Returns((string x,int i) => $"{x} {i}");

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change first argument in interceptor
                    invocation.SetArgument(0, "Altered String");
                    invocation.SetArgument(1, 50);
                    return next();
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestMethod2));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as ITestMethod2;

            var actualRv = instance!.TestMethod(null,100);

            Assert.AreEqual(expectedRv, actualRv);
            mock1.Verify();
            mockInterceptor.VerifyAll();

        }
    }
}
