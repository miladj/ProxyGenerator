using System;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using IInvocation = ProxyGenerator.Core.IInvocation;

namespace ProxyGenerator.Test
{
    public class RefMethodTest
    {
        
        delegate void TestMethodRef<T>(ref T bar);
        public interface IRefTestMethod
        {
            void TestMethod(ref int p);
        }
        public interface IRefGenericTestMethod
        {
            void TestMethod<T>(ref T p);
        }
        [Test]
        public void TestRefMethod()
        {
            Mock<IRefTestMethod> mock1 = new Mock<IRefTestMethod>();
 
            mock1.Setup(x => x.TestMethod(ref It.Ref<int>.IsAny)).Callback(new TestMethodRef<int>(((ref int bar) => bar = 100)));

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor
                    return next(); 
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(IRefTestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as IRefTestMethod;
            int id=0;
            instance!.TestMethod(ref id);

            Assert.AreEqual(100, id);

        }
        [Test]
        public void TestRefGenericTestMethod()
        {
            Mock<IRefGenericTestMethod> mock1 = new Mock<IRefGenericTestMethod>();

            mock1.Setup(x => x.TestMethod(ref It.Ref<int>.IsAny)).Callback(new TestMethodRef<int>(((ref int bar) => bar = 100)));

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor

                    return next();
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(IRefGenericTestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as IRefGenericTestMethod;
            int id = 1200;
            instance!.TestMethod(ref id);

            Assert.AreEqual(100, id);

        }
        [Test]
        public void TestChangeValueInInterceptorRefGenericTestMethod()
        {
            Mock<IRefGenericTestMethod> mock1 = new Mock<IRefGenericTestMethod>();
            
            int interceptorValue = 0;
            int beforeCallInterceptorValue = 0;

            mock1.Setup(x => x.TestMethod(ref It.Ref<int>.IsAny)).Callback(new TestMethodRef<int>(((ref int bar) => bar = 100)));

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor
                    beforeCallInterceptorValue = (int)invocation.GetArgument(0);
                    object o = next();
                    interceptorValue = (int) invocation.GetArgument(0);
                    invocation.SetArgument(0, 5600);
                    return o;
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(IRefGenericTestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as IRefGenericTestMethod;
            int id = 1200;
            instance!.TestMethod(ref id);

            Assert.AreEqual(1200, beforeCallInterceptorValue);
            Assert.AreEqual(100, interceptorValue);
            Assert.AreEqual(5600, id);

        }

    }
}