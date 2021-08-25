using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using IInvocation = ProxyGenerator.Core.IInvocation;

namespace ProxyGenerator.Test
{
    public class InterceptorTests
    {
        public interface ITestInterceptor
        {
            void Test();
            void Test(int param1);
            string Test(int param1,int id);


        }
        public class M : IInterceptor
        {
            public virtual object Intercept(IInvocation invocation,Func<object> next)
            {
                return next();
            }
        }
        
        [Test]
        public void TestMethodVoid()
        {
            
            var mock = new Mock<ITestInterceptor>();

            Type proxy = new Core.ProxyMaker(typeof(ITestInterceptor), new [] { typeof(M)}).CreateProxy();
            var instance = Activator.CreateInstance(proxy, mock.Object) as ITestInterceptor;
            
            instance!.Test(78);

            mock.Verify(x => x.Test(It.IsIn(78)));
        }
        
        [Test]
        public void TestMethodVoidPlusServiceProvider()
        {
            var mock = new Mock<ITestInterceptor>();
            var interceptorMock = new Mock<M>();
            interceptorMock.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next());
            ServiceCollection sc = new ServiceCollection();
            sc.AddSingleton(typeof(ITestInterceptor), mock.Object);
            sc.AddSingleton<M>(interceptorMock.Object);
            ServiceProvider buildServiceProvider = sc.BuildServiceProvider();

            Type proxy = new Core.ProxyMaker(typeof(ITestInterceptor), new[] { typeof(M) },true).CreateProxy();
            
            var instance = Activator.CreateInstance(proxy, buildServiceProvider) as ITestInterceptor;

            instance!.Test(78);

            mock.Verify(x => x.Test(It.IsIn(78)));
            
        }
        [Test]
        public void TestMethodWithReturnValue()
        {

            var mock = new Mock<ITestInterceptor>();
            const string expectedRv = "OK";
            mock.Setup(x => x.Test(90, 1200)).Returns(expectedRv);
            Type proxy = new Core.ProxyMaker(typeof(ITestInterceptor), new[] { typeof(M) }).CreateProxy();
            var instance = Activator.CreateInstance(proxy, mock.Object) as ITestInterceptor;

            var actualRv=instance!.Test(90,1200);

            Assert.AreEqual(expectedRv,actualRv);

        }
        [Test]
        public void TestMethodWithReturnValuePlusServiceProvider()
        {
            var mock = new Mock<ITestInterceptor>();
            const string expectedRv = "OK";
            mock.Setup(x => x.Test(90, 1200)).Returns(expectedRv);
            var interceptorMock = new Mock<M>();
            interceptorMock.Setup(x => x.Intercept(It.Is<IInvocation>(x=>x.Arguments[0].Equals(90) && x.Arguments[1].Equals(1200)), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next());
            ServiceCollection sc = new ServiceCollection();
            sc.AddSingleton(typeof(ITestInterceptor), mock.Object);
            sc.AddSingleton<M>(interceptorMock.Object);
            ServiceProvider buildServiceProvider = sc.BuildServiceProvider();

            Type proxy = new Core.ProxyMaker(typeof(ITestInterceptor), new[] { typeof(M) }, true).CreateProxy();

            var instance = Activator.CreateInstance(proxy, buildServiceProvider) as ITestInterceptor;

            var actualRv = instance!.Test(90, 1200);

            Assert.AreEqual(expectedRv, actualRv);
            

        }
        [Test]
        public void TestMethodWithoutParameterPlusServiceProvider()
        {
            var mock = new Mock<ITestInterceptor>();

           
            var interceptorMock = new Mock<M>();
            interceptorMock.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next());
            ServiceCollection sc = new ServiceCollection();
            sc.AddSingleton(typeof(ITestInterceptor), mock.Object);
            sc.AddSingleton<M>(interceptorMock.Object);
            ServiceProvider buildServiceProvider = sc.BuildServiceProvider();

            Type proxy = new Core.ProxyMaker(typeof(ITestInterceptor), new[] { typeof(M) }, true).CreateProxy();

            var instance = Activator.CreateInstance(proxy, buildServiceProvider) as ITestInterceptor;

            instance!.Test();

            mock.Verify(x=>x.Test());
            

        }
    }
}