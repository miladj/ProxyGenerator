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
    public class AsyncInterceptorTests
    {        
        public interface ITestAsyncMethod
        {
            Task<string> TestMethod(int id);
        }
        public class InterceptorAsyncTest : IInterceptorAsync
        {
            public async Task InterceptAsync(IInvocation invocation, Task invocationReturnValue)
            {
                await invocationReturnValue;
            }

            public async Task<T> InterceptAsync<T>(IInvocation invocation, Task<T> invocationReturnValue)
            {
                return await invocationReturnValue;
            }

            public async ValueTask InterceptAsync(IInvocation invocation, ValueTask invocationReturnValue)
            {
                await invocationReturnValue;
            }

            public async ValueTask<T> InterceptAsync<T>(IInvocation invocation, ValueTask<T> invocationReturnValue)
            {
                return await invocationReturnValue;
            }

            public object InterceptSync(IInvocation invocation, Func<object> next)
            {
                return next();
            }
        }
        
        public class AsyncTestMethod : ITestAsyncMethod
        {
            public async Task<string> TestMethod(int id)
            {
                return await Task.FromResult("OK");
            }
        }
        [Test]
        public async Task TestMethodWithAsyncTaskAsync()
        {
            const string expectedRv = "OK";
            //var mock = new Mock<ITestAsyncMethod>();
            //mock.Setup(x => x.TestMethod(90)).ReturnsAsync(expectedRv).Verifiable();

            var mockInterceptor = new Mock<IInterceptor>();
            var mockAsyncInterceptor = mockInterceptor.As<IInterceptorAsync>();
            mockAsyncInterceptor.Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Task<string>>())).Returns(async (IInvocation _, Task<string> x) => await x);
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) => {
                    return AsyncHelperExtensions.AsyncHelper(invocation, next, /*new InterceptorAsyncTest()*/mockAsyncInterceptor.Object);
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethod));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockInterceptor.Object }) as ITestAsyncMethod;

            var actualRv = await instance!.TestMethod(90);

            Assert.AreEqual(expectedRv, actualRv);
            //mock.VerifyAll();
            mockInterceptor.VerifyAll();

        }
    }
}
