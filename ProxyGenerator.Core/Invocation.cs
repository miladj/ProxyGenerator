using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public class Invocation:IInvocation
    {
        public object[] Arguments { get; set; }
        public MethodInfo Method { get; set; }
        public MethodInfo MethodInvocationTarget { get; set; }
        public object Original { get; set; }
        public Type TargetType { get; set; }

    }
}