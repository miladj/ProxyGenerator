using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public abstract class Invocation:IInvocation,IDefaultInvocation
    {
        public object[] Arguments { get; set; }
        public abstract MethodInfo Method { get; }

        public MethodInfo MethodInvocationTarget =>
            ProxyHelperMethods.GetImplMethodInfo(TargetType, Method);
        public object Original { get; set; }
        public Type TargetType => Original?.GetType();

        public abstract object Invoke();
    }
}