using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using System;
using IInvocation = ProxyGenerator.Core.IInvocation;

namespace ProxyGenerator.Test
{
    public class ChangeTargetInInterceptor
    {
        public interface ITestMethod
        {
            string TestMethod(int id);
        }


        [Test]
        public void TestChangeTargetInInterceptor()
        {
            const string expectedRv = "2";
            Mock<ITestMethod> mock1 = new Mock<ITestMethod>();
            Mock<ITestMethod> mock2 = new Mock<ITestMethod>();
            mock2.Setup(x => x.TestMethod(It.IsAny<int>())).Returns("2").Verifiable();

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor
                    invocation.Target = mock2.Object;
                    return next();
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as ITestMethod;

            var actualRv = instance!.TestMethod(90);

            Assert.AreEqual(expectedRv, actualRv);
            mock1.Verify(x => x.TestMethod(It.IsAny<int>()), Times.Never);
            mock2.Verify();
            mockInterceptor.VerifyAll();

        }
    }
}