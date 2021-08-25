using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public interface IInvocation
    {
        object[] Arguments { get; }

        MethodInfo Method { get; }

        MethodInfo MethodInvocationTarget { get; }

        object Original { get; }

        Type TargetType { get; }

    }
}