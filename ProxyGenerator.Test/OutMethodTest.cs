using System;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using IInvocation = ProxyGenerator.Core.IInvocation;


namespace ProxyGenerator.Test
{
    public class OutMethodTest
    {
        delegate void TestMethodOut<T>(out T bar);
        public interface IOutTestMethod
        {
            void TestMethod(out int p);
        }
        public interface IOutGenericTestMethod
        {
            void TestMethod<T>(out T p);
        }
        [Test]
        public void TestOutTestMethod()
        {
            Mock<IOutTestMethod> mock1 = new Mock<IOutTestMethod>();



            mock1.Setup(x => x.TestMethod(out It.Ref<int>.IsAny)).Callback(new TestMethodOut<int>(((out int bar) => bar=100)));

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor
                    return next();
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(IOutTestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as IOutTestMethod;
            int id ;
            instance!.TestMethod(out id);

            Assert.AreEqual(100,id);

        }
        [Test]
        public void TestOutGenericTestMethod()
        {
            Mock<IOutGenericTestMethod> mock1 = new Mock<IOutGenericTestMethod>();
            

            
            mock1.Setup(x => x.TestMethod(out It.Ref<int>.IsAny)).Callback(new TestMethodOut<int>(((out int bar) => bar = 100)));

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor
                    
                    return next();
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(IOutGenericTestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as IOutGenericTestMethod;
            int id=1200;
            instance!.TestMethod(out id);

            Assert.AreEqual(100, id);

        }
        [Test]
        public void TestChangeValueInInterceptorOutGenericTestMethod()
        {
            Mock<IOutGenericTestMethod> mock1 = new Mock<IOutGenericTestMethod>();
            int interceptorValue = 0;

            mock1.Setup(x => x.TestMethod(out It.Ref<int>.IsAny)).Callback(new TestMethodOut<int>(((out int bar) => bar = 100)));

            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    //change target in interceptor

                    object o = next();
                    interceptorValue = (int)invocation.GetArgument(0);
                    invocation.SetArgument(0,5600);
                    return o;
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(IOutGenericTestMethod));
            var instance = Activator.CreateInstance(proxy, mock1.Object, new IInterceptor[] { mockInterceptor.Object }) as IOutGenericTestMethod;
            int id = 1200;
            instance!.TestMethod(out id);

            Assert.AreEqual(100, interceptorValue);
            Assert.AreEqual(5600, id);

        }

    }
}
