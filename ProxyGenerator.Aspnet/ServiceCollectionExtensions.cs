using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ProxyGenerator.Core;

namespace ProxyGenerator.Aspnet
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Decorate<TService, TDecorator>(this IServiceCollection serviceCollection)
            where TDecorator : TService
        {
            return serviceCollection.Decorate(typeof(TService), typeof(TDecorator));
        }
        public static IServiceCollection Decorate(this IServiceCollection serviceCollection,Type serviceType,Type decoratorType)
        {
            for (var index = 0; index < serviceCollection.Count; index++)
            {
                ServiceDescriptor descriptor = serviceCollection[index];
                if (descriptor.ServiceType == serviceType)
                {
                    if(!IsOpenGeneric(descriptor.ServiceType))
                        serviceCollection[index] = CreateNewDescriptorForFactory(descriptor,
                            provider=>provider.GetOriginalInstanceForDecorator(descriptor,decoratorType));
                    else
                    {
                        if (descriptor.ImplementationType == null)
                            throw new NotSupportedException("Open Generics supports only ImplementationType");
                        Type proxy = new ProxyMakerAspnet(descriptor.ServiceType,descriptor.ImplementationType,decoratorType).CreateProxy();
                        serviceCollection[index] =
                            new ServiceDescriptor(descriptor.ServiceType, proxy, descriptor.Lifetime);
                    }
                }
            }
            return serviceCollection;
        }
        public static IServiceCollection Decorate(this IServiceCollection serviceCollection, Type serviceType, Type[] interceptorsType)
        {
            for (var index = 0; index < serviceCollection.Count; index++)
            {
                ServiceDescriptor descriptor = serviceCollection[index];
                if (descriptor.ServiceType == serviceType)
                {
                    if (!IsOpenGeneric(descriptor.ServiceType))
                    {
                        Type proxy = new ProxyMakerAspnet(descriptor.ServiceType).CreateProxy();
                        serviceCollection[index] = CreateNewDescriptorForFactory(descriptor,
                            provider => provider.GetOriginalInstanceForInterceptors(descriptor, proxy,
                                interceptorsType));
                    }
                    else
                    {
                        if (descriptor.ImplementationType == null)
                            throw new NotSupportedException("Open Generics supports only ImplementationType");
                        Type proxy = new ProxyMakerAspnet(descriptor.ServiceType, descriptor.ImplementationType,interceptorsType).CreateProxy();
                        serviceCollection[index] =
                            new ServiceDescriptor(descriptor.ServiceType, proxy, descriptor.Lifetime);
                    }
                }
            }
            return serviceCollection;
        }

        private static ServiceDescriptor CreateNewDescriptorForFactory(ServiceDescriptor oldDescriptor, Func<IServiceProvider, object> factory) =>
            new ServiceDescriptor(oldDescriptor.ServiceType, factory, oldDescriptor.Lifetime);
        private static object? GetDescriptorInstance(this IServiceProvider serviceProvider, ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationType != null)
                return serviceProvider.GetServiceOrCreateInstance(descriptor.ImplementationType);
            else if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;
            else if (descriptor.ImplementationFactory != null)
                return descriptor.ImplementationFactory(serviceProvider);
            else
                throw new NotSupportedException("unknown descriptor");
        }
        private static object? GetOriginalInstanceForDecorator(this IServiceProvider serviceProvider, ServiceDescriptor descriptor,
            Type decoratorType)
        {
            object[] arguments = new object[1];
            arguments[0] = serviceProvider.GetDescriptorInstance(descriptor);
            return serviceProvider.GetServiceOrCreateInstance(decoratorType, arguments);
        }

        private static object? GetOriginalInstanceForInterceptors(this IServiceProvider serviceProvider, ServiceDescriptor descriptor,Type proxyType, Type[] interceptorsType)
        {
            object[] arguments = new object[2];
            arguments[0] = serviceProvider.GetDescriptorInstance(descriptor);
            arguments[1] = interceptorsType.Select(inter => (IInterceptor)serviceProvider.GetServiceOrCreateInstance(inter)).ToArray();
            return serviceProvider.GetServiceOrCreateInstance(proxyType, arguments);
        }
        //ActivatorUtilities.cs 
        private static object GetServiceOrCreateInstance(this IServiceProvider provider, Type type, params object[] parameters)
        {
            return provider.GetService(type) ?? ActivatorUtilities.CreateInstance(provider, type,parameters);
        }
        public static bool IsOpenGeneric(Type type)
        {
            return type.IsGenericType;
        }
    }
}
