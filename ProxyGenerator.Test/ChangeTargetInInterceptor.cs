using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using IInvocation = ProxyGenerator.Core.IInvocation;
using ProxyGenerator.Core.Asyncs;
namespace ProxyGenerator.Test
{
    public class ChangeTargetInInterceptor
    {
        public interface ITestMethod
        {
            string TestMethod(int id);
        }

        public class Impl1 : ITestMethod
        {
            public string TestMethod(int id)
            {
                return "1";
            }
        }
        public class Impl2 : ITestMethod
        {
            public string TestMethod(int id)
            {
                return "2";
            }
        }
        [Test]
        public void TestChangeTargetInInterceptor()
        {
            const string expectedRv = "2";

            var mockInterceptor = new Mock<IInterceptor>();
            
            
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor
                    invocation.Target = new Impl2();
                    return next();
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestMethod));
            var instance = Activator.CreateInstance(proxy, new Impl1(), new IInterceptor[] { mockInterceptor.Object }) as ITestMethod;

            var actualRv = instance!.TestMethod(90);

            Assert.AreEqual(expectedRv, actualRv);
            //mock.VerifyAll();
            mockInterceptor.VerifyAll();

        }
    }
}
