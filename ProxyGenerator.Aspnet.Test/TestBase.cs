using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Scrutor.Tests
{
    public class TestBase
    {
        protected static ServiceProvider ConfigureProvider(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection();

            configure(services);

            return services.BuildServiceProvider();
        }

        public static T IsType<T>(object instance)
        {
            Assert.IsInstanceOf<T>(instance);
            return (T) instance;
        }
    }
}
