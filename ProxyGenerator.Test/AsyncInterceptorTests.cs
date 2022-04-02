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
            Task TestMethod(string id);
        }
        public interface ITestAsyncMethodWithReturnValue
        {
            Task<string> TestMethod(int id);
        }
        public interface ITestAsyncMethodValueTask
        {
            ValueTask TestMethodValueTask(decimal id);
        }
        public interface ITestAsyncMethodValueTaskWithReturnValue
        {
            ValueTask<int> TestMethodValueTask(int id, int id2);
        }
        public interface ITestAsyncMethodValueTaskGeneric
        {
            ValueTask<T> TestMethodValueTask2<T>(int id, int id2);
        }

        public class AsyncTestMethod :
            ITestAsyncMethod,
            ITestAsyncMethodWithReturnValue, 
            ITestAsyncMethodValueTaskGeneric, 
            ITestAsyncMethodValueTaskWithReturnValue, 
            ITestAsyncMethodValueTask
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

            public async ValueTask TestMethodValueTask(decimal id)
            {
                await Task.Delay(0);
            }

            public async Task TestMethod(string id)
            {
                await Task.Delay(0);
            }
        }
        [Test]
        public async Task TestMethodWithAsyncTaskAsync()
        {
            const string expectedRv = "90";

            var mockAsyncInterceptor = new Mock<IInterceptorAsync>();
            mockAsyncInterceptor.Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<Task<object>>>())).Returns(async (IInvocation _, Func<Task<object>> x) => await x());
            

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethodWithReturnValue));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockAsyncInterceptor.Object.CreateAsyncInterceptor() }) as ITestAsyncMethodWithReturnValue;

            var actualRv = await instance!.TestMethod(90);

            Assert.AreEqual(expectedRv, actualRv);

            mockAsyncInterceptor.VerifyAll();

        }
        [Test]
        public async Task TestMethodWithValueWithoutReturn()
        {
            var mockAsyncInterceptor = new Mock<IInterceptorAsync>();
            mockAsyncInterceptor.Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<Task<object>>>())).Returns(async (IInvocation _, Func<Task<object>> x) =>
            {
                object i = await x();
                return i;
            });

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethodValueTask));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockAsyncInterceptor.Object.CreateAsyncInterceptor() }) as ITestAsyncMethodValueTask;

            await instance!.TestMethodValueTask(10M);

            

            mockAsyncInterceptor.VerifyAll();

        }
        [Test]
        public async Task TestMethodWithTaskWithoutReturn()
        {
            var mockAsyncInterceptor = new Mock<IInterceptorAsync>();
            mockAsyncInterceptor.Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<Task<object>>>())).Returns(async (IInvocation _, Func<Task<object>> x) =>
            {
                object i = await x();
                return i;
            });

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethod));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockAsyncInterceptor.Object.CreateAsyncInterceptor() }) as ITestAsyncMethod;

            await instance!.TestMethod("");



            mockAsyncInterceptor.VerifyAll();

        }
        [Test]
        public async Task TestMethodWithAsyncValueTaskAsync()
        {
            var mockAsyncInterceptor = new Mock<IInterceptorAsync>();
            mockAsyncInterceptor.Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<Task<object>>>())).Returns(async (IInvocation _, Func<Task<object>> x) =>
            {
                object i = await x();
                return i;
            });

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethodValueTaskWithReturnValue));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockAsyncInterceptor.Object.CreateAsyncInterceptor() }) as ITestAsyncMethodValueTaskWithReturnValue;

            var actualRv = await instance!.TestMethodValueTask(90, 10);

            Assert.AreEqual(100, actualRv);

            mockAsyncInterceptor.VerifyAll();

        }
        [Test]
        public async Task TestMethodWithAsyncValueTaskGenericAsync()
        {

            
            var mockAsyncInterceptor = new Mock<IInterceptorAsync>();
            mockAsyncInterceptor.Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<Task<object>>>())).Returns(async (IInvocation _, Func<Task<object>> x) =>
            {
                var i = await x();
                return i;
            });
            

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethodValueTaskGeneric));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockAsyncInterceptor.Object.CreateAsyncInterceptor() }) as ITestAsyncMethodValueTaskGeneric;

            var actualRv = await instance!.TestMethodValueTask2<int>(90, 10);

            Assert.AreEqual(100, actualRv);

            mockAsyncInterceptor.VerifyAll();

        }
        [Test]
        public async Task TestMethodWithAsyncTask_ChangeArgument()
        {
            const string expectedRv = "100";

            
            var mockAsyncInterceptor = new Mock<IInterceptorAsync>();
            mockAsyncInterceptor
                .Setup(x => x.InterceptAsync(It.IsAny<IInvocation>(), It.IsAny<Func<Task<object>>>()))
                .Returns(async (IInvocation invocation, Func<Task<object>> x) =>
                {
                    invocation.SetArgument(0,100);//change first parameter to 100 so the result should be 100
                    return await x();
                });

            Type proxy = ProxyMaker.CreateProxyType(typeof(ITestAsyncMethodWithReturnValue));
            var instance = Activator.CreateInstance(proxy, new AsyncTestMethod(), new IInterceptor[] { mockAsyncInterceptor.Object.CreateAsyncInterceptor() }) as ITestAsyncMethodWithReturnValue;

            var actualRv = await instance!.TestMethod(90);

            Assert.AreEqual(expectedRv, actualRv);

            mockAsyncInterceptor.VerifyAll();

        }
    }
}
