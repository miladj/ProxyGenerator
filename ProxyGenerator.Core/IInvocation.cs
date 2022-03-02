using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public interface IInvocation
    {
        object[] Arguments { get; }

        MethodInfo Method { get; }

        MethodInfo MethodInvocationTarget { get; }

        object Target { get; set; }

        Type TargetType { get; }

    }
}