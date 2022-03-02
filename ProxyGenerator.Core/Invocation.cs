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

        public object Original
        {
            get => _target;
            set => _target = value;
        }

        protected object _target;

        public Type TargetType => Original?.GetType();

        public abstract object Invoke();
    }
}