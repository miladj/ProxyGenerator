using System;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using ProxyGenerator.Core;
using static ProxyGenerator.Aspnet.Test.ServiceCollectionHelper;
using IInvocation = ProxyGenerator.Core.IInvocation;

namespace ProxyGenerator.Aspnet.Test
{
    public interface ISimple
    {
        string Test();
    }

    public class Simple : ISimple
    {
        public string Test()
        {
            return "OK";
        }
    }
    public class SimpleDecorator : ISimple
    {
        private readonly ISimple _service;

        public SimpleDecorator(ISimple service)
        {
            _service = service;
        }

        public string Test()
        {
            return _service.Test();
        }
    }

    public interface ISimple<T>
    {
        string Test();
    }

    public class Simple<T> : ISimple<T>
    {
        public bool IsCalled { get; private set; }
        public string Test()
        {
            IsCalled = true;
            return "OK";
        }
    }
    public class SimpleDecorator<T> : ISimple<T>
    {
        private readonly ISimple<T> _service;

        public SimpleDecorator(ISimple<T> service)
        {
            _service = service;
        }

        public string Test()
        {
            return _service.Test();
        }
    }

    public class DecorationTest
    {
        [Test]
        public void SimpleTest()
        {
            IServiceProvider serviceCollection = CreateServiceCollection(services =>
            {
                services.AddTransient<ISimple, Simple>();
                services.Decorate<ISimple, SimpleDecorator>();
            });
            var service = serviceCollection.GetService<ISimple>();
            string test = service.Test();
            Assert.AreEqual("OK",test);
            Assert.IsInstanceOf<SimpleDecorator>(service);
        }
        [Test]
        public void SimpleTest_Interceptor()
        {
            Mock<IInterceptor> mockInterceptor = new Mock<IInterceptor>();
            Mock<ISimple> mockSimple = new Mock<ISimple>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next());
            IServiceProvider serviceCollection = CreateServiceCollection(services =>
            {
                services.AddSingleton(typeof(ISimple),mockSimple.Object);
                services.AddSingleton(mockInterceptor.Object.GetType(),mockInterceptor.Object);
                services.Intercept(typeof(ISimple),mockInterceptor.Object.GetType());
            });
            
            var service = serviceCollection.GetService<ISimple>();
            Assert.IsNotNull(service);
            service.Test();
            mockInterceptor.Verify(x=>x.Intercept(It.IsAny<IInvocation>(),It.IsAny<Func<object>>()));
            mockSimple.Verify(x=>x.Test());
        }

        [Test]
        public void SimpleTest_CloseGeneric()
        {
            IServiceProvider serviceCollection = CreateServiceCollection(services =>
            {
                services.AddTransient(typeof(ISimple<int>), typeof(Simple<int>));
                services.Decorate(typeof(ISimple<>), typeof(SimpleDecorator<>));
            });
            var service = serviceCollection.GetService(typeof(ISimple<int>)) as ISimple<int>;
            string test = service.Test();
            Assert.AreEqual("OK", test);
            Assert.IsNotInstanceOf<Simple<int>>(service);
            Assert.IsInstanceOf<SimpleDecorator<int>>(service);
            
        }
        [Test]
        public void SimpleTest_CloseGeneric_Interceptor()
        {
            Mock<IInterceptor> mockInterceptor = new Mock<IInterceptor>();
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next());

            Mock<ISimple<int>> mockSimple = new Mock<ISimple<int>>();
            mockSimple.Setup(x => x.Test()).Returns("OK");

            IServiceProvider serviceCollection = CreateServiceCollection(services =>
            {
                services.AddSingleton(typeof(ISimple<int>), mockSimple.Object);
                services.AddSingleton(mockInterceptor.Object.GetType(), mockInterceptor.Object);
                services.Intercept(typeof(ISimple<>), mockInterceptor.Object.GetType());
            });

            var service = serviceCollection.GetService<ISimple<int>>();
            Assert.IsNotNull(service);
            string test = service.Test();
            Assert.AreEqual("OK", test);
            mockInterceptor.Verify(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()));

        }

        [Test]
        public void SimpleTest_OpenGeneric()
        {
            IServiceProvider serviceCollection = CreateServiceCollection(services =>
            {
                services.AddTransient(typeof(ISimple<>), typeof(Simple<>));
                services.Decorate(typeof(ISimple<>), typeof(SimpleDecorator<>));
            });
            var service = serviceCollection.GetService(typeof(ISimple<int>)) as ISimple<int>;
            string test = service.Test();
            Assert.AreEqual("OK",test);
            Assert.IsNotInstanceOf<SimpleDecorator<int>>(service);
            Assert.IsNotInstanceOf<Simple<int>>(service);
        }
        [Test]
        public void SimpleTest_OpenGeneric_Interceptor()
        {
            Mock<IInterceptor> mockInterceptor = new Mock<IInterceptor>();
            
            mockInterceptor.Setup(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()))
                .Returns((IInvocation _, Func<object> next) => next());
            IServiceProvider serviceCollection = CreateServiceCollection(services =>
            {
                services.AddSingleton(typeof(ISimple<>),typeof(Simple<>));
                services.AddSingleton(mockInterceptor.Object.GetType(), mockInterceptor.Object);
                services.Intercept(typeof(ISimple<>), mockInterceptor.Object.GetType());
            });

            var service = serviceCollection.GetService<ISimple<int>>();
            Assert.IsNotNull(service);
            string test = service.Test();
            Assert.AreEqual("OK",test);
            mockInterceptor.Verify(x => x.Intercept(It.IsAny<IInvocation>(), It.IsAny<Func<object>>()));
            
        }
    }
}
