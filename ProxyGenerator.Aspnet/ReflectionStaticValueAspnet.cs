using System;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace ProxyGenerator.Core
{
    public static partial class ReflectionStaticValue
    {
        public static readonly MethodInfo Array_Empty_OfObjet =
            typeof(Array).GetMethod(nameof(Array.Empty))!.MakeGenericMethod(typeof(object));

        public static readonly MethodInfo ActivatorCreateInstanceUtilities =
            typeof(Microsoft.Extensions.DependencyInjection.ActivatorUtilities).GetMethods()
                .First(x => !x.IsGenericMethod);
    }
}
