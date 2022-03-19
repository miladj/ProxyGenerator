using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public interface IInvocation
    {
        long ArgumentCount { get; }

        MethodInfo Method { get; }

        MethodInfo MethodInvocationTarget { get; }

        object Target { get; set; }

        Type TargetType { get; }

        void SetArgument(uint index, object value);
        object GetArgument(uint index);

    }
}