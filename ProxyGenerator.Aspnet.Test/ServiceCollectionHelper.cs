using System;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyGenerator.Aspnet.Test
{
    public static class ServiceCollectionHelper
    {
        public static IServiceProvider CreateServiceCollection(Action<IServiceCollection> configureAction)
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            configureAction(serviceCollection);
            return serviceCollection.BuildServiceProvider();
        }
    }
}
