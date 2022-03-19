using System;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using IInvocation = ProxyGenerator.Core.IInvocation;

namespace ProxyGenerator.Test
{
    public class PassThroughInterceptor : IInterceptor
    {
        public virtual object Intercept(IInvocation invocation, Func<object> next)
        {
            return next();
        }
    }
    public class InterceptorTests
    {
        public interface ITestInterceptor
        {
            void Test();
            void Test(int param1);
            string Test(int param1,int id);


        }
        public interface ITestInterceptor2
        {
            string Test(int param1, int param2, int param3, int param4, int param5, int param6);
        }

        [Test]
        public void TestMethodVoid()
        {
            
            var mock = new Mock<ITestInterceptor>();
            
            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestInterceptor));
            var instance = Activator.CreateInstance(proxy, mock.Object,new []{new PassThroughInterceptor()}) as ITestInterceptor;
            
            instance!.Test(78);

            mock.Verify(x => x.Test(It.IsIn(78)));
        }
        
        [Test]
        public void TestMethodVoidPlusServiceProvider()
        {
            var mock = new Mock<ITestInterceptor>();
            var interceptorMock = new Mock<PassThroughInterceptor>();
            interceptorMock.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next());
            ServiceCollection sc = new ServiceCollection();
            sc.AddSingleton(typeof(ITestInterceptor), mock.Object);
            sc.AddSingleton<PassThroughInterceptor>(interceptorMock.Object);
            ServiceProvider buildServiceProvider = sc.BuildServiceProvider();

            Type proxy = Core.ProxyMaker.CreateProxyType(typeof(ITestInterceptor));
            ParameterInfo[] parameterInfos = proxy.GetConstructors()[0].GetParameters();
            var instance = Activator.CreateInstance(proxy, new object[] { buildServiceProvider.GetRequiredService<ITestInterceptor>(),new IInterceptor[]{ buildServiceProvider.GetRequiredService<PassThroughInterceptor>()}}) as ITestInterceptor;

            instance!.Test(78);

            mock.Verify(x => x.Test(It.IsIn(78)));
            
        }
        [Test]
        public void TestMethodWithReturnValue()
        {

            var mock = new Mock<ITestInterceptor>();
            const string expectedRv = "OK";
            mock.Setup(x => x.Test(90, 1200)).Returns(expectedRv);
            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestInterceptor));
            var instance = Activator.CreateInstance(proxy, mock.Object,Array.Empty<IInterceptor>()) as ITestInterceptor;

            var actualRv=instance!.Test(90,1200);

            Assert.AreEqual(expectedRv,actualRv);

        }
        [Test]
        public void TestMethodWithReturnValuePlusServiceProvider()
        {
            var mock = new Mock<ITestInterceptor>();
            const string expectedRv = "OK";
            mock.Setup(x => x.Test(90, 1200)).Returns(expectedRv);
            var interceptorMock = new Mock<PassThroughInterceptor>();
            interceptorMock.Setup(x => x.Intercept(It.Is<IInvocation>(x=>x.GetArgument(0).Equals(90) && x.GetArgument(1).Equals(1200)), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next());
            ServiceCollection sc = new ServiceCollection();
            sc.AddSingleton(typeof(ITestInterceptor), mock.Object);
            sc.AddSingleton<PassThroughInterceptor>(interceptorMock.Object);
            ServiceProvider buildServiceProvider = sc.BuildServiceProvider();
            
            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestInterceptor));

            var instance = Activator.CreateInstance(proxy, new object[] { buildServiceProvider.GetRequiredService<ITestInterceptor>(), new IInterceptor[] { buildServiceProvider.GetRequiredService<PassThroughInterceptor>() } }) as ITestInterceptor;

            var actualRv = instance!.Test(90, 1200);

            Assert.AreEqual(expectedRv, actualRv);
            

        }
        [Test]
        public void TestMethodWithoutParameterPlusServiceProvider()
        {
            var mock = new Mock<ITestInterceptor>();

           
            var interceptorMock = new Mock<PassThroughInterceptor>();
            interceptorMock.Setup(x => x.Intercept(It.Is<IInvocation>(x=>x.Target==mock.Object && x.MethodInvocationTarget==mock.Object.GetType().GetMethod(nameof(ITestInterceptor.Test),Array.Empty<Type>())), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next());
            ServiceCollection sc = new ServiceCollection();
            sc.AddSingleton(typeof(ITestInterceptor), mock.Object);
            sc.AddSingleton<PassThroughInterceptor>(interceptorMock.Object);
            ServiceProvider buildServiceProvider = sc.BuildServiceProvider();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestInterceptor));

            var instance = Activator.CreateInstance(proxy, new object[] { buildServiceProvider.GetRequiredService<ITestInterceptor>(), new IInterceptor[] { buildServiceProvider.GetRequiredService<PassThroughInterceptor>() } }) as ITestInterceptor;

            instance!.Test();

            mock.Verify(x=>x.Test());
            

        }
        [Test]
        public void TestMethod_ManyParameters()
        {
            var mock = new Mock<ITestInterceptor2>();
            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next()).Verifiable();
            mock.Setup(x => x.Test(It.IsIn(1), It.IsIn(2), It.IsIn(3), It.IsIn(4), It.IsIn(5), It.IsIn(6))).Returns("OK").Verifiable();
            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestInterceptor2));
            var instance = Activator.CreateInstance(proxy, new object[]{mock.Object,new IInterceptor[]{ mockInterceptor.Object}}) as ITestInterceptor2;
            Assert.IsNotNull(instance);

            string test = instance.Test(1,2,3,4,5,6);
            Assert.AreEqual("OK",test);

            mock.VerifyAll();
            mockInterceptor.VerifyAll();
        }
        [Test]
        public void TestMethod_ManyParameters_CheckInvocationData()
        {
            
            var mock = new Mock<ITestInterceptor2>();
            var mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) =>
                {
                    Assert.AreEqual(invocation.ArgumentCount,6);
                    for (uint i = 0; i < 6; i++)
                    {
                        Assert.AreEqual(invocation.GetArgument(i), i+1);
                    }
                    
                    return next();
                }).Verifiable();
            mock.Setup(x => x.Test(It.IsIn(1), It.IsIn(2), It.IsIn(3), It.IsIn(4), It.IsIn(5), It.IsIn(6))).Returns("OK").Verifiable();
            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestInterceptor2));
            var instance = Activator.CreateInstance(proxy, new object[] { mock.Object, new IInterceptor[] { mockInterceptor.Object } }) as ITestInterceptor2;
            Assert.IsNotNull(instance);

            string test = instance.Test(1, 2, 3, 4, 5, 6);
            Assert.AreEqual("OK", test);

            mock.VerifyAll();
            mockInterceptor.VerifyAll();
        }
    }
}