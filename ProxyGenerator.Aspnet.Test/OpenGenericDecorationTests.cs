using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ProxyGenerator.Aspnet.Test
{
    public interface IQueryHandler<TQuery, TResult> { }
    public class OpenGenericDecorationTests : TestBase
    {
        [Test]
        public void CanDecorateOpenGenericTypeBasedOnClass()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<QueryHandler<MyQuery, MyResult>, MyQueryHandler>();
                services.Decorate(typeof(QueryHandler<,>), typeof(LoggingQueryHandler<,>));
                services.Decorate(typeof(QueryHandler<,>), typeof(TelemetryQueryHandler<,>));
            });

            var instance = provider.GetRequiredService<QueryHandler<MyQuery, MyResult>>();

            var telemetryDecorator = IsType<TelemetryQueryHandler<MyQuery, MyResult>>(instance);
            var loggingDecorator = IsType<LoggingQueryHandler<MyQuery, MyResult>>(telemetryDecorator.Inner);
            IsType<MyQueryHandler>(loggingDecorator.Inner);
        }


        [Test]
        public void CanDecorateOpenGenericTypeBasedOnInterface()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IQueryHandler<MyQuery,MyResult>, MyQueryHandler>();
                services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));
                services.Decorate(typeof(IQueryHandler<,>), typeof(TelemetryQueryHandler<,>));
            });

            var instance = provider.GetRequiredService<IQueryHandler<MyQuery, MyResult>>();

            var telemetryDecorator = IsType<TelemetryQueryHandler<MyQuery, MyResult>>(instance);
            var loggingDecorator = IsType<LoggingQueryHandler<MyQuery, MyResult>>(telemetryDecorator.Inner);
            IsType<MyQueryHandler>(loggingDecorator.Inner);
        }

        

        [Test]
        public void CanDecorateOpenGenericTypeBasedOnGrandparentInterface()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<ISpecializedQueryHandler, MySpecializedQueryHandler>();
                services.AddSingleton<IQueryHandler<MyQuery, MyResult>, MySpecializedQueryHandler>();
                services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));
            });

            var instance = provider.GetRequiredService<IQueryHandler<MyQuery, MyResult>>();

            var loggingDecorator = IsType<LoggingQueryHandler<MyQuery, MyResult>>(instance);
            IsType<MySpecializedQueryHandler>(loggingDecorator.Inner);
        }

        [Test]
        public void DecoratingOpenGenericTypeBasedOnGrandparentInterfaceDoesNotDecorateParentInterface()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<ISpecializedQueryHandler, MySpecializedQueryHandler>();
                services.AddSingleton<IQueryHandler<MyQuery, MyResult>, MySpecializedQueryHandler>();
                services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingQueryHandler<,>));
            });

            var instance = provider.GetRequiredService<ISpecializedQueryHandler>();

            IsType<MySpecializedQueryHandler>(instance);
        }

        //[Test]
        public void OpenGenericDecoratorsSkipOpenGenericServiceRegistrations()
        {
            // var provider = ConfigureProvider(services =>
            // {
            //     services.Scan(x =>
            //         x.FromAssemblyOf<Message>()
            //             .AddClasses(classes => classes
            //                 .AssignableTo(typeof(IMessageProcessor<>)))
            //             .AsImplementedInterfaces()
            //             .WithTransientLifetime());
            //
            //     services.Decorate(typeof(IMessageProcessor<>), typeof(GenericDecorator<>));
            // });
            //
            // var processor = provider.GetRequiredService<IMessageProcessor<Message>>();
            //
            // var decorator = IsType<GenericDecorator<Message>>(processor);
            //
            // IsType<MessageProcessor>(decorator.Decoratee);
        }

        [Test]
        public void OpenGenericDecoratorsCanBeConstrained()
        {
            var provider = ConfigureProvider(services =>
            {
                services.AddSingleton<IQueryHandler<MyQuery, MyResult>, MyQueryHandler>();
                services.AddSingleton<IQueryHandler<MyConstrainedQuery, MyResult>, MyConstrainedQueryHandler>();
                services.Decorate(typeof(IQueryHandler<,>), typeof(ConstrainedDecoratorQueryHandler<,>));
            });


            var instance = provider.GetRequiredService<IQueryHandler<MyQuery, MyResult>>();
            var constrainedInstance = provider.GetRequiredService<IQueryHandler<MyConstrainedQuery, MyResult>>();

            IsType<MyQueryHandler>(instance);
            IsType<ConstrainedDecoratorQueryHandler<MyConstrainedQuery,MyResult>>(constrainedInstance);
        }
    }

    // ReSharper disable UnusedTypeParameter

    public class MyQuery { }

    public class MyResult { }

    public class MyQueryHandler : QueryHandler<MyQuery, MyResult> { }

    public class QueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult> { }

    public interface MyConstraint<out TResult> { }

    public class MyConstrainedQuery : MyConstraint<MyResult> { }

    public class MyConstrainedQueryHandler : QueryHandler<MyConstrainedQuery, MyResult> { }

    public class ConstrainedDecoratorQueryHandler<TQuery, TResult> : DecoratorQueryHandler<TQuery, TResult> 
        where TQuery : MyConstraint<TResult>
    {
        public ConstrainedDecoratorQueryHandler(IQueryHandler<TQuery, TResult> inner) : base(inner) { }
    }

    public class LoggingQueryHandler<TQuery, TResult> : DecoratorQueryHandler<TQuery, TResult>
    {
        public LoggingQueryHandler(IQueryHandler<TQuery, TResult> inner) : base(inner) { }
    }

    public class TelemetryQueryHandler<TQuery, TResult> : DecoratorQueryHandler<TQuery, TResult>
    {
        public TelemetryQueryHandler(IQueryHandler<TQuery, TResult> inner) : base(inner) { }
    }

    public class DecoratorQueryHandler<TQuery, TResult> : QueryHandler<TQuery, TResult>, IDecoratorQueryHandler<TQuery, TResult>
    {
        public DecoratorQueryHandler(IQueryHandler<TQuery, TResult> inner)
        {
            Inner = inner;
        }

        public IQueryHandler<TQuery, TResult> Inner { get; }
    }

    public interface IDecoratorQueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult>
    {
        IQueryHandler<TQuery, TResult> Inner { get; }
    }

    public interface ISpecializedQueryHandler : IQueryHandler<MyQuery, MyResult> { }

    public class MySpecializedQueryHandler : ISpecializedQueryHandler { }

    public interface IMessageProcessor<T> { }

    public class Message { }

    public class MessageProcessor : IMessageProcessor<Message> { }

    public class GenericDecorator<T> : IMessageProcessor<T>
    {
        public GenericDecorator(IMessageProcessor<T> decoratee)
        {
            Decoratee = decoratee;
        }

        public IMessageProcessor<T> Decoratee { get; }
    }
}
