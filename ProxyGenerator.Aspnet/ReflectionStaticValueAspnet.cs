using System;
using System.Linq;
using System.Reflection;
using ProxyGenerator.Aspnet;

// ReSharper disable once CheckNamespace
namespace ProxyGenerator.Core
{
    public static partial class ReflectionStaticValue
    {
        public static readonly MethodInfo Array_Empty_OfObject =
            typeof(Array).GetMethod(nameof(Array.Empty))!.MakeGenericMethod(typeof(object));

        public static readonly MethodInfo ActivatorGetServiceOrCreateInstance =
            typeof(ActivatorUtilitiesExtraMethod).GetMethod(nameof(ActivatorUtilitiesExtraMethod.GetServiceOrCreateInstance));
        public static readonly MethodInfo ActivatorCreateInstanceUtilities =
            typeof(Microsoft.Extensions.DependencyInjection.ActivatorUtilities).GetMethods()
                .First(x => !x.IsGenericMethod);
    }
}
