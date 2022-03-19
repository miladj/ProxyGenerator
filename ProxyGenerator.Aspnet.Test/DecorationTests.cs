using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProxyGenerator.Core;

namespace ProxyGenerator.Aspnet.Test
{
    public class DecorationTests : TestBase
    {
        [Test]
        public void CanDecorateType()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IDecoratedService, Decorated>();

                services.Decorate<IDecoratedService, Decorator>();
            });

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = IsType<Decorator>(instance);

            IsType<Decorated>(decorator.Inner);
        }
        
        [Test]
        public void CanDecorateMultipleLevels()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IDecoratedService, Decorated>();

                services.Decorate<IDecoratedService, Decorator>();
                services.Decorate<IDecoratedService, Decorator>();
            });

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = IsType<Decorator>(instance);
            var outerDecorator = IsType<Decorator>(decorator.Inner);

            IsType<Decorated>(outerDecorator.Inner);
        }

        [Test]
        public void CanDecorateDifferentServices()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IDecoratedService, Decorated>();
                services.AddSingleton<IDecoratedService, OtherDecorated>();

                services.Decorate<IDecoratedService, Decorator>();
            });

            var instances = provider
                .GetRequiredService<IEnumerable<IDecoratedService>>()
                .ToArray();
            
            Assert.AreEqual(2, instances.Length);
            Assert.That(instances, Is.All.InstanceOf<Decorator>());
        }

        [Test]
        public void ShouldReplaceExistingServiceDescriptor()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IDecoratedService, Decorated>();

            services.Decorate<IDecoratedService, Decorator>();

            // var descriptor = services.GetDescriptor<IDecoratedService>();
            var descriptor = services.First(x => x.ServiceType == typeof(IDecoratedService));

            Assert.AreEqual(typeof(IDecoratedService), descriptor.ServiceType);
            Assert.NotNull(descriptor.ImplementationFactory);
        }

        [Test]
        public void CanDecorateExistingInstance()
        {
            var existing = new Decorated();

            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IDecoratedService>(existing);

                services.Decorate<IDecoratedService, Decorator>();
            });

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = IsType<Decorator>(instance);
            var decorated = IsType<Decorated>(decorator.Inner);

            Assert.AreSame(existing, decorated);
        }

        [Test]
        public void CanInjectServicesIntoDecoratedType()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IService, SomeRandomService>();
                services.AddSingleton<IDecoratedService, Decorated>();

                services.Decorate<IDecoratedService, Decorator>();
            });

            var validator = provider.GetRequiredService<IService>();

            var instance = provider.GetRequiredService<IDecoratedService>();

            var decorator = IsType<Decorator>(instance);
            var decorated = IsType<Decorated>(decorator.Inner);

            Assert.AreSame(validator, decorated.InjectedService);
        }

        [Test]
        public void CanInjectServicesIntoDecoratingType()
        {
            var serviceProvider = ConfigureProvider(services =>
            {
                services.AddSingleton<IService, SomeRandomService>();
                services.AddSingleton<IDecoratedService, Decorated>();

                services.Decorate<IDecoratedService, Decorator>();
            });

            var validator = serviceProvider.GetRequiredService<IService>();

            var instance = serviceProvider.GetRequiredService<IDecoratedService>();

            var decorator = IsType<Decorator>(instance);

            Assert.AreSame(validator, decorator.InjectedService);
        }

        [Test]
        public void DisposableServicesAreDisposed()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddTransient<IDisposableService, DisposableService>();
                services.Decorate<IDisposableService, DisposableServiceDecorator>();
            });

            var disposable = provider.GetRequiredService<IDisposableService>();

            var decorator = IsType<DisposableServiceDecorator>(disposable);

            provider.Dispose();

            Assert.True(decorator.WasDisposed);
            Assert.True(decorator.Inner.WasDisposed);
        }

        [Test]
        public void ServicesWithSameServiceTypeAreOnlyDecoratedOnce()
        {
            // See issue: https://github.com/khellang/Scrutor/issues/125

            bool IsHandlerButNotDecorator(Type type)
            {
                var isHandlerDecorator = false;

                var isHandler = type.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEventHandler<>)
                );

                if (isHandler)
                {
                    isHandlerDecorator = type.GetInterfaces().Any(i => i == typeof(IHandlerDecorator));
                }

                return isHandler && !isHandlerDecorator;
            }
            
            var provider = ConfigureProvider(services =>
            {
                // This should end up with 3 registrations of type IEventHandler<MyEvent>.
                
                foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(x=>IsHandlerButNotDecorator(x)))
                {
                    foreach (Type @interface in type.GetInterfaces())
                    {
                        services.AddTransient(@interface, type);
                    }

                    
                }
                // services.Scan(s =>
                //     s.FromAssemblyOf<DecorationTests>()
                //         .AddClasses(c => c.Where(IsHandlerButNotDecorator))
                //         .AsImplementedInterfaces()
                //         .WithTransientLifetime());

                // This should not decorate each registration 3 times.
                services.Decorate(typeof(IEventHandler<>), typeof(MyEventHandlerDecorator<>));
            });

            var instances = provider.GetRequiredService<IEnumerable<IEventHandler<MyEvent>>>().ToList();

            Assert.AreEqual(3, instances.Count);
            
            
            Assert.That(instances,Is.All.Matches(new Predicate<IEventHandler<MyEvent>>(instance =>
            {
                var decorator = IsType<MyEventHandlerDecorator<MyEvent>>(instance);

                // The inner handler should not be a decorator.
                Assert.IsNotInstanceOf<MyEventHandlerDecorator<MyEvent>>(decorator.Handler);

                // The return call count should only be 1, we've only called Handle on one decorator.
                // If there were nested decorators, this would return a higher call count as it
                // would increment at each level.
                Assert.AreEqual(1, decorator.Handle(new MyEvent()));
                return true;
            })));
            
        }

        public interface IDecoratedService
        {
            void Test();
        }

        public interface IService { }

        private class SomeRandomService : IService { }

        public class Decorated : IDecoratedService
        {
            public Decorated(IService injectedService = null)
            {
                InjectedService = injectedService;
            }

            public IService InjectedService { get; }
            public void Test()
            {
                Console.WriteLine("Orig");
            }
        }

        public class Decorator : IDecoratedService
        {
            public Decorator(IDecoratedService inner, IService injectedService = null)
            {
                Inner = inner;
                InjectedService = injectedService;
            }

            public IDecoratedService Inner { get; }

            public IService InjectedService { get; }
            public void Test()
            {
                Inner.Test();
            }
        }

        public class OtherDecorated : IDecoratedService
        {
            public void Test()
            {
                
            }
        }

        private interface IDisposableService : IDisposable
        {
            bool WasDisposed { get; }
        }

        private class DisposableService : IDisposableService
        {
            public bool WasDisposed { get; private set; }

            public virtual void Dispose()
            {
                WasDisposed = true;
            }
        }

        private class DisposableServiceDecorator : IDisposableService
        {
            public DisposableServiceDecorator(IDisposableService inner)
            {
                Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            public IDisposableService Inner { get; }

            public bool WasDisposed { get; private set; }

            public void Dispose()
            {
                Inner.Dispose();
                WasDisposed = true;
            }
        }

        public interface IEvent
        {
        }

        public interface IEventHandler<in TEvent> where TEvent : class, IEvent
        {
            int Handle(TEvent @event);
        }

        public interface IHandlerDecorator
        {
        }

        public sealed class MyEvent : IEvent
        {}

        internal sealed class MyEvent1Handler : IEventHandler<MyEvent>
        {
            private int _callCount;

            public int Handle(MyEvent @event)
            {
                return _callCount++;
            }
        }

        internal sealed class MyEvent2Handler : IEventHandler<MyEvent>
        {
            private int _callCount;

            public int Handle(MyEvent @event)
            {
                return _callCount++;
            }
        }

        internal sealed class MyEvent3Handler : IEventHandler<MyEvent>
        {
            private int _callCount;

            public int Handle(MyEvent @event)
            {
                return _callCount++;
            }
        }

        internal sealed class MyEventHandlerDecorator<TEvent> : IEventHandler<TEvent>, IHandlerDecorator where TEvent: class, IEvent
        {
            public readonly IEventHandler<TEvent> Handler;

            public MyEventHandlerDecorator(IEventHandler<TEvent> handler)
            {
                Handler = handler;
            }

            public int Handle(TEvent @event)
            {
                return Handler.Handle(@event) + 1;
            }
        }
        public interface IDecoratedService<T>
        {
            void Test();
        }


        public class Decorated<T> : IDecoratedService<T>
        {
            public Decorated(IService injectedService = null)
            {
                InjectedService = injectedService;
            }

            public IService InjectedService { get; }
            public void Test()
            {
                Console.WriteLine("Orig");
            }
        }

        public class Decorator<T> : IDecoratedService<T>
        {
            public Decorator(IDecoratedService<T> inner, IService injectedService = null)
            {
                Inner = inner;
                InjectedService = injectedService;
            }

            public IDecoratedService<T> Inner { get; }

            public IService InjectedService { get; }
            public void Test()
            {
                Console.WriteLine("Decorator");
                Inner.Test();
            }
        }
        public class PassThrough:IInterceptor
        {
            public object Intercept(IInvocation invocation, Func<object> next)
            {
                return next();
            }
        }
        [Test]
        public void CanDecorateTypeOpenGeneric()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton(typeof(IDecoratedService<>), typeof(Decorated<>));

                services.Decorate(typeof(IDecoratedService<>), typeof(Decorator<>));
            });

            var instance = provider.GetRequiredService<IDecoratedService<int>>();
            instance.Test();
            Assert.IsNotInstanceOf<Decorator<int>>(instance);
        }
        [Test]
        public void CanDecorateTypeGeneric()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton(typeof(IDecoratedService<int>), typeof(Decorated<int>));

                services.Decorate(typeof(IDecoratedService<>), typeof(Decorator<>));
            });

            var instance = provider.GetRequiredService<IDecoratedService<int>>();
            instance.Test();
            Assert.IsInstanceOf<Decorator<int>>(instance);
        }
        [Test]
        public void CanDecorateTypeInterceptor()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton(typeof(IDecoratedService), typeof(Decorated));
                services.AddTransient<PassThrough>();

                services.Intercept(typeof(IDecoratedService),new Type[]{typeof(PassThrough)});
            });

            var instance = provider.GetRequiredService<IDecoratedService>();
            instance.Test();
            Assert.IsNotInstanceOf<Decorator<int>>(instance);
            Assert.IsNotInstanceOf<Decorated<int>>(instance);
        }
        [Test]
        public void CanDecorateTypeInterceptorOpenGeneric()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton(typeof(IDecoratedService<>), typeof(Decorated<>));
                services.AddTransient<PassThrough>();

                services.Intercept(typeof(IDecoratedService<>), new Type[] { typeof(PassThrough) });
            });

            var instance = provider.GetRequiredService<IDecoratedService<int>>();
            instance.Test();
            Assert.IsNotInstanceOf<Decorator<int>>(instance);
            Assert.IsNotInstanceOf<Decorated<int>>(instance);
        }
        [Test]
        public void CanDecorateTypeGenericInterceptor()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton(typeof(IDecoratedService<int>), typeof(Decorated<int>));

                services.Intercept(typeof(IDecoratedService<>), new Type[] { typeof(PassThrough) });
            });

            var instance = provider.GetRequiredService<IDecoratedService<int>>();
            instance.Test();
            // Assert.IsInstanceOf<Decorator<int>>(instance);
        }
    }
}
