using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
            void TestMethod2<T>(out T id);
            void TestMethod3(ref int id);
        }
        public interface ITestMethod2
        {
            void TestMethod2<T>(T id);
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
            mock1.Verify(x=>x.TestMethod(It.IsAny<int>()),Times.Never);
            mock2.Verify();
            mockInterceptor.VerifyAll();

        }
        delegate void test(ref int id);
        delegate void test2<T>(out T id);
        [Test]
        public void TestChangeTargetInInterceptor2()
        {
            Mock<ITestMethod> mock1 = new Mock<ITestMethod>();
            //Thread.Sleep(10*1000);
            ITestMethod interfaceProxyWithoutTarget = new Castle.DynamicProxy.ProxyGenerator().CreateInterfaceProxyWithTarget(typeof(ITestMethod),mock1.Object) as ITestMethod;
            int u=0;
            interfaceProxyWithoutTarget.TestMethod3(ref u);
            const string expectedRv = "2";
            
            mock1.Setup(x => x.TestMethod3(ref It.Ref<int>.IsAny)).Callback(new test(((ref int i) =>
            {
                i = 100;
            })));

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor
                   
                    return next();
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as ITestMethod;
            int id=0;
            instance!.TestMethod3(ref id);

            

        }
        [Test]
        public void TestChangeTargetInInterceptor4()
        {
            Mock<ITestMethod> mock1 = new Mock<ITestMethod>();
            //Thread.Sleep(10 * 1000);
            ITestMethod interfaceProxyWithoutTarget = new Castle.DynamicProxy.ProxyGenerator().CreateInterfaceProxyWithTarget(typeof(ITestMethod), mock1.Object) as ITestMethod;
            int u = 0;
            interfaceProxyWithoutTarget.TestMethod3(ref u);
            const string expectedRv = "2";

            mock1.Setup(x => x.TestMethod2(out It.Ref<int>.IsAny)).Callback(new test2<int>(((out int i) =>
            {
                i = 100;
            })));

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor

                    return next();
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as ITestMethod;
            
            instance!.TestMethod2<int>(out var id);



        }
        public class ITestMethodD : ITestMethod
        {
            private object k;
            public string TestMethod(int id)
            {
                throw new NotImplementedException();
            }

            public void TestMethod2<T>(out T id)
            {
                id = default;
                k = id;
            }

            public void TestMethod3(ref int id)
            {
                k = id;
            }
        }
        [Test]
        public void TestChangeTargetInInterceptor3()
        {
            Mock<ITestMethod2> mock1 = new Mock<ITestMethod2>();
            //Thread.Sleep(10*1000);
            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor

                    return next();
                }).Verifiable();
            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestMethod2));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as ITestMethod2;

            instance!.TestMethod2<object>(1);



        }
    }
}
