using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using System;
using System.Diagnostics;
using System.Threading;
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
            ValueTask<int> TestMethodValueTask(int id,int id2);
            ValueTask<T> TestMethodValueTask2<T>(int id,int id2);
        }

        public class AsyncTestMethod : ITestAsyncMethod
        {
            public async Task<string> TestMethod(int id)
            {
                return await Task.FromResult(id.ToString());
            }

            public async ValueTask<int> TestMethodValueTask(int id,int id2)
            {
                await Task.Delay(0);
                return id+id2;
            }

            public async ValueTask<T> TestMethodValueTask2<T>(int id, int id2)
            {
                await Task.Delay(0);
                if (id+id2 is T m)
                    return m;
                return default;
            }
        }
        [Test]
        public async Task TestMethodWithAsyncTaskAsync()
        {
            const string expectedRv = "90";

            var mockInterceptor = new Mock<IInterceptor>();
            var mockAsyncInterceptor = mockInterceptor.As<IInterceptorAsync>();
            mockAsyncInterceptor.Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<Task<string>>>())).Returns(async (IInvocation _, Func<Task<string>> x) => await x());
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) => {
                    return AsyncHelperExtensions.AsyncHelper(invocation, next, mockAsyncInterceptor.Object);
                }).Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethod));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockInterceptor.Object }) as ITestAsyncMethod;

            var actualRv = await instance!.TestMethod(90);

            Assert.AreEqual(expectedRv, actualRv); 

            mockInterceptor.VerifyAll();

        }
        [Test]
        public async Task TestMethodWithAsyncValueTaskAsync()
        {

            var mockInterceptor = new Mock<IInterceptor>();
            var mockAsyncInterceptor = mockInterceptor.As<IInterceptorAsync>();
            mockAsyncInterceptor.Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<ValueTask<int>>>())).Returns(async (IInvocation _, Func<ValueTask<int>> x) =>
            {
                int i = await x();
                return i;
            });
            mockInterceptor
                .Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) => AsyncHelperExtensions.AsyncHelper(invocation, next, mockAsyncInterceptor.Object))
                .Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethod));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockInterceptor.Object }) as ITestAsyncMethod;

            var actualRv = await instance!.TestMethodValueTask(90,10);

            Assert.AreEqual(100, actualRv);
            
            mockInterceptor.VerifyAll();

        }
        [Test]
        public async Task TestMethodWithAsyncValueTaskGenericAsync()
        {

            var mockInterceptor = new Mock<IInterceptor>();
            var mockAsyncInterceptor = mockInterceptor.As<IInterceptorAsync>();
            mockAsyncInterceptor.Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<ValueTask<int>>>())).Returns(async (IInvocation _, Func<ValueTask<int>> x) =>
            {
                int i = await x();
                return i;
            });
            mockInterceptor
                .Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) => AsyncHelperExtensions.AsyncHelper(invocation, next, mockAsyncInterceptor.Object))
                .Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethod));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockInterceptor.Object }) as ITestAsyncMethod;

            var actualRv = await instance!.TestMethodValueTask2<int>(90, 10);

            Assert.AreEqual(100, actualRv);

            mockInterceptor.VerifyAll();

        }
        [Test]
        public async Task TestMethodWithAsyncTask_ChangeArgument()
        {
            const string expectedRv = "100";

            var mockInterceptor = new Mock<IInterceptor>();
            var mockAsyncInterceptor = mockInterceptor.As<IInterceptorAsync>();
            mockAsyncInterceptor
                .Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<Task<string>>>()))
                .Returns(async (IInvocation invocation, Func<Task<string>> x) =>
                {
                    invocation.SetArgument(0,100);//change first parameter to 100 so the result should be 100
                    return await x();
                });
            mockInterceptor
                .Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation invocation, Func<object> next) => AsyncHelperExtensions.AsyncHelper(invocation, next, mockAsyncInterceptor.Object))
                .Verifiable();

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethod));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockInterceptor.Object }) as ITestAsyncMethod;

            var actualRv = await instance!.TestMethod(90);

            Assert.AreEqual(expectedRv, actualRv);

            mockInterceptor.VerifyAll();

        }
    }
}
