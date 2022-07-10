using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyGenerator.Aspnet
{
    public static class ActivatorUtilitiesExtraMethod
    {
        public static object GetServiceOrCreateInstance(this IServiceProvider provider, Type type, params object[] parameters)
        {
            return provider.GetService(type) ?? ActivatorUtilities.CreateInstance(provider, type, parameters);
        }
    }
}
