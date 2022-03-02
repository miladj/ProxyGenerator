using System;
using System.Reflection;

namespace ProxyGenerator.Core
{
    public abstract class Invocation:IInvocation,IDefaultInvocation
    {
        public Invocation()
        {

        }
        public object[] Arguments { get; set; }
        public abstract MethodInfo Method { get; }

        public MethodInfo MethodInvocationTarget =>
            ProxyHelperMethods.GetImplMethodInfo(TargetType, Method);

        public object Target
        {
            get => _target;
            set => _target = value;
        }

        protected object _target;

        public Type TargetType => Target?.GetType();

        public abstract object Invoke();
    }
}