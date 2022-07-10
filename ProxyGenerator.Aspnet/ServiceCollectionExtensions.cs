using System;
using System.Linq;
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
            if (!IsAssignableTo(decoratorType, serviceType))
                throw new NotSupportedException("DecoratorType must implement ServiceType");
            for (var index = 0; index < serviceCollection.Count; index++)
            {
                
                    ServiceDescriptor descriptor = serviceCollection[index];
                    if (AreSameType(serviceType, descriptor.ServiceType))
                    {
                        if (IsOpenGeneric(descriptor.ServiceType))
                        {
                            if (descriptor.ImplementationType == null)
                                throw new NotSupportedException("Open Generics supports only ImplementationType");
                            Type proxy = ProxyMakerAspnet.CreateProxyTypeWithDecorator(descriptor.ServiceType, descriptor.ImplementationType, decoratorType);
                            serviceCollection[index] =
                                new ServiceDescriptor(descriptor.ServiceType, proxy, descriptor.Lifetime);
                        }
                        else
                        {
                            if (IsOpenGeneric(decoratorType))
                            {
                                try
                                {
                                    decoratorType =
                                        decoratorType.MakeGenericType(descriptor.ServiceType.GenericTypeArguments);
                                }
                                catch (ArgumentException)
                                {
                                    continue;
                                }
                            }

                            serviceCollection[index] = CreateNewDescriptorForFactory(descriptor,
                                provider => provider.GetOriginalInstanceForDecorator(descriptor, decoratorType));
                                
                        }
                    }
                

            }
            return serviceCollection;
        }

        private static bool AreSameType(Type type1, Type type2)
        {
            if (type2 == type1) return true;
            return type1.IsGenericType && type2.IsGenericType &&
                   type1.GetGenericTypeDefinition() == type2.GetGenericTypeDefinition();
        }

        public static IServiceCollection Intercept(this IServiceCollection serviceCollection, Type serviceType, params Type[] interceptorsType)
        {
            for (var index = 0; index < serviceCollection.Count; index++)
            {
                ServiceDescriptor descriptor = serviceCollection[index];
                if (AreSameType(serviceType, descriptor.ServiceType))
                {
                    if (IsOpenGeneric(descriptor.ServiceType))
                    {
                        if (descriptor.ImplementationType == null)
                            throw new NotSupportedException("Open Generics supports only ImplementationType");
                        Type proxy = ProxyMakerAspnet.CreateProxyTypeWithInterceptors(descriptor.ServiceType, descriptor.ImplementationType, interceptorsType);
                        serviceCollection[index] =
                            new ServiceDescriptor(descriptor.ServiceType, proxy, descriptor.Lifetime);
                    }
                    else
                    {

                        Type proxy = ProxyMaker.CreateProxyType(descriptor.ServiceType);
                        serviceCollection[index] = CreateNewDescriptorForFactory(descriptor,
                            provider => provider.GetOriginalInstanceForInterceptors(descriptor, proxy,
                                interceptorsType));
                    }
                }
            }
            return serviceCollection;
        }

        private static ServiceDescriptor CreateNewDescriptorForFactory(ServiceDescriptor oldDescriptor, Func<IServiceProvider, object> factory) =>
            new ServiceDescriptor(oldDescriptor.ServiceType, factory, oldDescriptor.Lifetime);
        private static object GetDescriptorInstance(this IServiceProvider serviceProvider, ServiceDescriptor descriptor)
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
        private static object GetOriginalInstanceForDecorator(this IServiceProvider serviceProvider, ServiceDescriptor descriptor,
            Type decoratorType)
        {
            object[] arguments = new object[1];
            arguments[0] = serviceProvider.GetDescriptorInstance(descriptor);
            return serviceProvider.GetServiceOrCreateInstance(decoratorType, arguments);
        }

        private static object GetOriginalInstanceForInterceptors(this IServiceProvider serviceProvider, ServiceDescriptor descriptor,Type proxyType, Type[] interceptorsType)
        {
            object[] arguments = new object[2];
            arguments[0] = serviceProvider.GetDescriptorInstance(descriptor);
            arguments[1] = interceptorsType.Select(inter => (IInterceptor)serviceProvider.GetServiceOrCreateInstance(inter)).ToArray();
            return serviceProvider.GetServiceOrCreateInstance(proxyType, arguments);
        }
        
        
        public static bool IsOpenGeneric(Type type)
        {
            return type.IsGenericTypeDefinition;
        }
        public static bool IsAssignableTo(Type srcType, Type destType)
        {
            if (destType.IsAssignableFrom(srcType))
                return true;
            if (AreSameType(srcType, destType))
            {
                return true;
            }

            if (srcType.IsGenericType && srcType.GetGenericTypeDefinition() == destType)
                return true;

            var interfaceTypes = srcType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == destType)
                    return true;
            }

            

            Type baseType = srcType.BaseType;
            if (baseType == null) return false;

            return IsAssignableTo(baseType, destType);
        }
    }
}
